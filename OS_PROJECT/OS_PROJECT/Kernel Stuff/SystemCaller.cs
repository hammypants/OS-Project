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

        public static void DiskDump(Driver k)
        {
            StreamWriter writer = new StreamWriter(@"C:\Users\Cory\Documents\Visual Studio 2010\Projects\GitProjects\OS-Project\OS_PROJECT\OS_PROJECT\Phase Two Dumps\DiskDump.txt", false);
            //Console.WriteLine("Writing disk to file...");
            for (uint page = 0; page < 512; page++)
            {
                for (uint offset = 0; offset < 4; offset++)
                {
                    if (page < 10)
                    {
                        writer.Write("[00" + page + "]");
                    }
                    else if (page < 100)
                    {
                        writer.Write("[0" + page + "]");
                    }
                    else
                    {
                        writer.Write("[" + page + "]");
                    }
                    writer.Write("[" + offset + "]");
                    writer.Write("[" + SystemCaller.ConvertInputDataToHexstring(k.Disk.ReadDataFromDisk((page * 4) + offset)) + "]");
                    writer.WriteLine();
                }
            }
            writer.Close();
        }

        public static void ProcessMemoryDump(Driver k, PCB pcb)
        {
            lock (fileLock)
            {
                StreamWriter writer = new StreamWriter(@"C:\Users\Cory\Documents\Visual Studio 2010\Projects\GitProjects\OS-Project\OS_PROJECT\OS_PROJECT\Phase Two Dumps\Process" + pcb.ProcessID + "MemoryDump.txt", false);
                //Console.WriteLine("Writing process memory to file...");
                uint beginning_page = pcb.PageTable.ReturnFirstPage(pcb);
                uint number_of_pages = pcb.JobLength / 4;
                uint frame_to_write;
                for (uint page = beginning_page; page < beginning_page + number_of_pages + 1; page++)
                {
                    frame_to_write = pcb.PageTable.table[page].Frame;
                    for (uint offset = 0; offset < 4; offset++)
                    {
                        if (frame_to_write < 10)
                        {
                            writer.Write("[00" + frame_to_write + "]");
                        }
                        else if (frame_to_write < 100)
                        {
                            writer.Write("[0" + frame_to_write + "]");
                        }
                        else
                        {
                            writer.Write("[" + frame_to_write + "]");
                        }
                        writer.Write("[" + offset + "]");
                        if (pcb.PageTable.Lookup(page).InMemory)
                        {
                            writer.Write("[" + SystemCaller.ConvertInputDataToHexstring(MMU.Read((frame_to_write * 4) + offset)) + "]");
                        }
                        else writer.Write("[0]");
                        writer.WriteLine();
                    }
                }
                writer.Close();
            }
        }

        public static void CoreDump(Driver k)
        {
            StreamWriter writer = new StreamWriter(//@"C:\Users\Cory\Documents\Visual Studio 2010\Projects\GitProjects\OS-Project\OS_PROJECT\OS_PROJECT\CoreDumpBatch" + batchNumber + ".txt", false);
                @"\\cse6\student\crichers\OS-Project\OS_PROJECT\OS_PROJECT\CoreDump.txt", false);
            Console.WriteLine("--------------------------------------");
            Console.WriteLine("CORE DUMP -- WRITING MEMORY TO FILE:");
            Console.WriteLine("CoreDump.txt");
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

        public static void CoreDumpProcessCompletionWaitingTimes(Driver k, int batchNumber)
        {
            StreamWriter writer = new StreamWriter(@"C:\Users\Cory\Documents\Visual Studio 2010\Projects\GitProjects\OS-Project\OS_PROJECT\OS_PROJECT\CoreDump-CompTimes" + batchNumber + ".txt", true);
            foreach (Process p in k.deadProcesses)
            {
                writer.WriteLine(p.PCB.ProcessID + " , " + p.PCB.completionTime);
            }
            writer.Close();
            writer = new StreamWriter(@"C:\Users\Cory\Documents\Visual Studio 2010\Projects\GitProjects\OS-Project\OS_PROJECT\OS_PROJECT\CoreDump-WaitTimes" + batchNumber + ".txt", true);
            foreach (Process p in k.deadProcesses)
            {
                writer.WriteLine(p.PCB.ProcessID + " , " + p.PCB.waitingTime);
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

    }
}
