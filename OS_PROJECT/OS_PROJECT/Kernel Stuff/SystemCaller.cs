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

        public static void CoreDump(Driver k, int batchNumber)
        {
            StreamWriter writer = new StreamWriter(//@"C:\Users\Cory\Documents\Visual Studio 2010\Projects\GitProjects\OS-Project\OS_PROJECT\OS_PROJECT\CoreDumpBatch" + batchNumber + ".txt", false);
                @"\\cse6\student\crichers\OS-Project\OS_PROJECT\OS_PROJECT\CoreDumpBatch" + batchNumber + ".txt", false);
            Console.WriteLine("--------------------------------------");
            Console.WriteLine("CORE DUMP -- WRITING MEMORY TO FILE:");
            Console.WriteLine("CoreDumpBatch" + batchNumber + ".txt");
            Console.WriteLine("--------------------------------------");
            for (uint i = 0; i < k.RAM.GetMemorySize(); i++)
            {
                if (i < 10)
                {
                    writer.Write("[000" + i + "]");
                }
                else if (i < 100)
                {
                    writer.Write("[00" + i + "]");
                }
                else if (i < 1000)
                {
                    writer.Write("[0" + i + "]");
                }
                else
                {
                    writer.Write("[" + i + "]");
                }
                writer.Write(SystemCaller.ConvertInputDataToHexstring(k.RAM.ReadDataFromMemory(i)));
                //writer.Write(k.RAM.ReadDataFromMemory(i));
                writer.WriteLine();
            }
            writer.Close();
        }

        public static void CoreDumpByProccess(Driver k, int batchNumber)
        {
            StreamWriter writer = new StreamWriter(//@"C:\Users\Cory\Documents\Visual Studio 2010\Projects\GitProjects\OS-Project\OS_PROJECT\OS_PROJECT\CoreDumpProcsBatch" + batchNumber + ".txt", false);
                @"\\cse6\student\crichers\OS-Project\OS_PROJECT\OS_PROJECT\CoreDumpProcsBatch" + batchNumber + ".txt", false);
            Console.WriteLine("--------------------------------------");
            Console.WriteLine("CORE DUMP -- WRITING MEMORY TO FILE:");
            Console.WriteLine("CoreDumpProcsBatch" + batchNumber + ".txt");
            Console.WriteLine("--------------------------------------");
            foreach (Process p in k.deadProcesses)
            {
                writer.WriteLine("--------------------------------------");
                if (p.PCB.ProcessID > 9)
                {
                    writer.WriteLine("----PROCESS #" + p.PCB.ProcessID + "-----------------------");
                }
                else
                writer.WriteLine("----PROCESS #"+p.PCB.ProcessID+"------------------------");
                writer.WriteLine("--------------------------------------");
                for (uint i = p.PCB.MemoryAddress; i < (p.PCB.MemoryAddress + p.PCB.JobLength); i++)
                {
                    if (i < 10)
                    {
                        writer.Write("[000" + i + "]");
                    }
                    else if (i < 100)
                    {
                        writer.Write("[00" + i + "]");
                    }
                    else if (i < 1000)
                    {
                        writer.Write("[0" + i + "]");
                    }
                    else
                    {
                        writer.Write("[" + i + "]");
                    }
                    writer.Write(SystemCaller.ConvertInputDataToHexstring(k.RAM.ReadDataFromMemory(i)));
                    writer.WriteLine();
                }
            }
            writer.Close();
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
