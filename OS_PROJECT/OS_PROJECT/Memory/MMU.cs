using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OS_PROJECT
{
    class MMU
    {
        private static MMU singleton;
        Driver kernel;

        static FrameTableLocation[] frame_table = new FrameTableLocation[256];

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
                frame_table[iterator].Free = true;
                frame_table[iterator].Page = 513;
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

        struct FrameTableLocation
        {
            public uint Page;
            public bool Free;
        }
    }
}
