using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Globalization;

namespace OS_PROJECT
{
    class Loader
    {
        Driver kernel;

        Disk disk;
        RAM RAM;
        NewProcessQueue NPQ;

        uint jobNum = 0;
        uint numWords = 0;
        uint priorityNum = 0;
        uint otptBuffSize = 14;
        uint tempBuffSize = 14;
        uint inputBuffSize = 14;
        uint addressCounter = 0;
        uint currentJobStartAddress = 0;

        public Loader(Driver k)
        {
            kernel = k;
            this.disk = k.Disk;
            this.RAM = k.RAM;
            this.NPQ = k.NewProcessQueue;
        }

        protected void ReadProgramFile()
        {
            string line;

            string job = "JOB";
            string end = "END";
            string data = "Data";

            //Read the file
            StreamReader file = new StreamReader(@"\\cse6\student\crichers\OS-Project\OS_PROJECT\OS_PROJECT\DataFile2.txt");
                //@"C:\Users\Cory\Documents\Visual Studio 2010\Projects\GitProjects\OS-Project\OS_PROJECT\OS_PROJECT\DataFile2.txt");

            while ((line = file.ReadLine()) != null)
            {
                string[] words = line.Split(' ');

                string fwdSlashes = words[0];
                string jobDataEnd = words[1];

                if (jobDataEnd.Equals(end)) Console.WriteLine(jobDataEnd);

                // if job do ...
                if (string.Compare(jobDataEnd, job) == 0)
                {
                    jobNum = Convert.ToUInt32(words[2], 16);
                    numWords = Convert.ToUInt32(words[3], 16);
                    priorityNum = Convert.ToUInt32(words[4], 16);
                    currentJobStartAddress = addressCounter;

                    //read then send numWords to screen/disk
                    for (int i = 1; i <= numWords; i++)
                    {
                        disk.WriteDataToDisk(addressCounter++, SystemCaller.ConvertInputDataToUInt(file.ReadLine()));
                    }
                }

                // if data do ...
                if (string.Compare(jobDataEnd, data) == 0)
                {
                    numWords = 44; //one;
                    otptBuffSize = Convert.ToUInt32(words[3], 16);
                    tempBuffSize = Convert.ToUInt32(words[4], 16);

                    //Console.WriteLine("Data#");

                    for (int i = 1; i <= numWords; i++)
                    {
                        disk.WriteDataToDisk(addressCounter++, SystemCaller.ConvertInputDataToUInt(file.ReadLine()));
                    }
                    file.ReadLine();
                }

                if (string.Compare(jobDataEnd, end) == 1)
                {
                    Console.WriteLine("Job: " + jobNum);
                    SpawnProcess(jobNum, priorityNum, numWords, otptBuffSize, inputBuffSize, tempBuffSize, currentJobStartAddress);
                }
            }
            file.Close();

            jobNum = 0;
            numWords = 0;
            priorityNum = 0;
        }

        void SpawnProcess(uint pID, uint priority, uint numWords, uint outB, uint inB, uint tempB, uint startAddress)
        {
            Process p = new Process(new PCB());
            p.PCB.ProcessID = pID;
            p.PCB.Priority = priorityNum;
            p.PCB.InstructionLength = numWords;
            p.PCB.OutputBufferSize = UInt32.Parse("C", NumberStyles.HexNumber); 
            p.PCB.TempBufferSize = UInt32.Parse("C", NumberStyles.HexNumber); 
            p.PCB.InputBufferSize = UInt32.Parse("14", NumberStyles.HexNumber);
            p.PCB.DiskAddress = startAddress;
            Console.WriteLine("Disk Address: " + p.PCB.DiskAddress.ToString());
            Console.WriteLine("Input Buffer: " +p.PCB.InputBufferSize.ToString());
            Console.WriteLine("Instruction Length: " + p.PCB.InstructionLength.ToString());
            Console.WriteLine("Output Buffer: " + p.PCB.OutputBufferSize.ToString());
            Console.WriteLine("Temp Buffer: " + p.PCB.TempBufferSize.ToString());
            Console.WriteLine("Priority: " + p.PCB.Priority.ToString());
            NPQ.AccessQueue.Enqueue(p);
            Console.WriteLine("Process spawned!");
            Console.WriteLine();
        }

        public void Run()
        {
            ReadProgramFile();
        }
    }
}
