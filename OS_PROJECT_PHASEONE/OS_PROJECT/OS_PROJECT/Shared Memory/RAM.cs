using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OS_PROJECT
{
    class RAM
    {
        protected uint[] RAM_Memory = new uint[1028];

        bool locked;
        public bool Locked
        { get { return locked; } set { locked = value; } }

        public RAM()
        { }

        public void WriteDataToMemory(uint physicalAddress, uint data)
        {
            try { RAM_Memory[physicalAddress] = data; }
            catch { Console.WriteLine("Could not write to specified memory location. Please check for out of bounds errors."); }
        }

        public uint ReadDataFromMemory(uint physicalAddress)
        {
            try { return RAM_Memory[physicalAddress]; }
            catch
            { Console.WriteLine("Could not read data from memory. Please check for out of bounds errors.");
                return 0; }
        }

        public int GetMemorySize()
        {
            return RAM_Memory.GetLength(0);
        }
    }
}
