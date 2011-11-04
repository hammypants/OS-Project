using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Threading;
using System.IO;

namespace OS_PROJECT
{
    class SystemCaller
    {
        static volatile Object fileLock = new Object();
        static volatile Object anotherFileLock = new Object();

        public SystemCaller()
        { }

        public static uint ConvertInputDataToUInt(string input)
        {
            string splitInput = input.Substring(2);
            uint converted = UInt32.Parse(splitInput, NumberStyles.HexNumber);
            return converted;
        }

        public static uint ConvertHexstringToUInt(string input)
        {
            uint converted = UInt32.Parse(input, NumberStyles.HexNumber);
            return converted;
        }

        public static string ConvertInputDataToHexstring(uint input)
        {
            string output = String.Format("{0:X}", input);
            return output;
        }

        public static void DisplayContentsOfDisk(Disk d)
        {
            for (int iterator = 0; iterator < d.GetDiskSize() - 1900; iterator++)
            {
                uint _uint = d.ReadDataFromDisk((uint)iterator);
                string _data = String.Format("{0:X}", _uint);
                Console.WriteLine(_data);
            }
        }

        public static void DisplayContentsOfRAM(RAM r)
        {
            for (int iterator = 0; iterator < r.GetMemorySize() - 963; iterator++)
            {
                uint _uint = r.ReadDataFromMemory((uint)iterator);
                string _data = String.Format("{0:X}", _uint);
                Console.WriteLine(_data);
            }
        }

        public static void DisplayMemoryOfProcess(Process p, RAM r)
        {
            for (uint iterator = p.PCB.MemoryAddress; iterator < p.PCB.JobLength; iterator++)
            {
                uint _uint = r.ReadDataFromMemory((uint)iterator);
                string _data = String.Format("{0:X}", _uint);
                Console.WriteLine(_data);
            }
        }

        public static void WriteToFile(string s)
        {
            lock (fileLock)
            {
                StreamWriter file = File.AppendText("osProj.txt");
                file.WriteLine(s);
                file.Close();
            }
        }

        public static void WriteRAMToFile(RAM r)
        {
            lock (anotherFileLock)
            {
                StreamWriter file = File.AppendText("osProj.txt");
                file.WriteLine(r);
                file.Close();
            }
        }

    }
}
