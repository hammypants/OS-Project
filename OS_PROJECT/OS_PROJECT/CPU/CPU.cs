using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Diagnostics;

namespace OS_PROJECT
{
    enum Instruction
    { 
        RD, WR, ST, LW, MOV, ADD, SUB, MUL, DIV, AND, OR, MOVI, ADDI, MULI, DIVI, LDI,
        SLT, SLTI, HLT, NOP, JMP, BEQ, BNE, BEZ, BNZ, BGZ, BLZ 
    }

    enum InstructionType
    {
        IO, I, R, J, NOP
    }

    class CPU
    {
        bool faulted = false;
        Driver kernel;
        Disk disk;
        RAM RAM;
        ReadyQueue RQ;
        PCB cpuPCB;
        public PCB CPU_PCB
        { get { return cpuPCB; } set { cpuPCB = value; } }
        Process currentProcess;
        public Process CurrentProcess
        { get { return currentProcess; } set { currentProcess = value; } }

        Thread thread;
        public Thread CPUThread
        { get { return thread; } }

        ManualResetEventSlim _suspendEvent;
        public bool isActive;
        int id;
        int processCounter;
        Stopwatch totalElapsedTime = new Stopwatch();

        volatile Object dispatcherLock = new Object();
        volatile Object cacheLock = new Object();

        uint[] cache = new uint[1];

        string currentInstruction;
        Instruction currentInstructionOp;
        InstructionType currentInstructionOpType;

        public CPU(Driver k, int id)
        {
            kernel = k;
            disk = k.Disk;
            RAM = k.RAM;
            RQ = k.ReadyQueue;
            cpuPCB = new PCB();
            cache = new uint[1];
            this.id = id;
            thread = new Thread(new ThreadStart(this.Run));
            _suspendEvent = new ManualResetEventSlim(false);
            thread.Start();
        }

        public void RunCPU()
        {
            thread.Start();
            isActive = true;
        }

        public void PauseCPU()
        {
            Console.WriteLine("Pausing CPU " + id + " -- The number of processes currently completed by this CPU is : " + processCounter + ".\n");
            _suspendEvent.Reset();
            isActive = false;
        }

        public void ResumeCPU()
        {
            Console.WriteLine("Awakening CPU " + id + ".\n");
            _suspendEvent.Set();
            isActive = true;
        }

        public void Run()
        {
            while (true)
            {
                _suspendEvent.Wait(Timeout.Infinite);

                if (!HasProcess())
                {
                    GetProcess();

                    if (!HasProcess())
                    {
                        PauseCPU();
                    }
                }
                while (HasProcess())
                {
                    faulted = false;
                    Fetch();
                    if (!faulted)
                    Decode();
                    if (!faulted)
                    Execute();
                }
            }
        }

        void GetProcess()
        {
            lock (dispatcherLock)
            {
                if (InterruptHandler.ServicedProcessesCount() == 0)
                {
                    kernel.Dispatcher.DispatchProcess(this);
                    if (currentProcess != null)
                    {
                        cpuPCB._waitingTime.Stop();
                        cpuPCB.waitingTime = cpuPCB._waitingTime.Elapsed.TotalMilliseconds;
                        Console.WriteLine("Job " + currentProcess.PCB.ProcessID + " (Priority: " + currentProcess.PCB.Priority + " | Instr. Length: " + currentProcess.PCB.InstructionLength +
                            ") spent " + cpuPCB._waitingTime.Elapsed.TotalMilliseconds.ToString() + "ms waiting.\n");
                        processCounter++;
                        FillInitalCache();
                        totalElapsedTime.Start();
                    }
                }
                else
                {
                    currentProcess = InterruptHandler.DequeueProcess();
                    cpuPCB = currentProcess.PCB;
                }
            }
        }

        void FillInitalCache()
        {
            //cache = new uint[currentProcess.PCB.JobLength];
            //lock (cacheLock)
            //{
            //    for (uint iterator = 0; iterator < cache.GetLength(0); iterator++)
            //    {
            //        cache[iterator] = RAM.ReadDataFromMemory(currentProcess.PCB.MemoryAddress + iterator);
            //    }
            //}
            lock (cacheLock)
            {
                uint firstPage = (uint)Array.FindIndex<PageTable.PageTableLocation>(cpuPCB.PageTable.table, e => e.IsOwned == true);
                uint frame;
                for (uint i = firstPage; i < 4; i++)
                {
                    frame = cpuPCB.PageTable.table[i].Frame;
                    cpuPCB.Cache_Instruction.Write(MMU.ReadFrame(frame), i);
                    cpuPCB.Cache_Instruction.MapFrame(i, frame);
                }
            }
        }

        void Fetch()
        {
            //currentInstruction = SystemCaller.ConvertInputDataToHexstring(cache[cpuPCB.ProgramCounter++]);
            currentInstruction = SystemCaller.ConvertInputDataToHexstring(ReadFromCache(cpuPCB.ProgramCounter++));
            if (currentProcess == null)
                faulted = true;
        }

        void Decode()
        {
            if (currentInstruction.Length == 7)
            {
                currentInstruction = "0" + currentInstruction;
            }
            string currentInstructionCopy = currentInstruction;
            string firstTwoHexChars = currentInstructionCopy.Substring(0, 2);
            currentInstructionCopy = currentInstructionCopy.Substring(2, 6);
            currentInstruction = currentInstructionCopy;

            #region Instr Switch
            switch (firstTwoHexChars)
            {
                case "C0"://RD
                    currentInstructionOpType = InstructionType.IO;
                    currentInstructionOp = Instruction.RD;
                    break;
                case "C1"://WR
                    currentInstructionOpType = InstructionType.IO;
                    currentInstructionOp = Instruction.WR;
                    break;
                case "42"://ST
                    currentInstructionOpType = InstructionType.I;
                    currentInstructionOp = Instruction.ST;
                    break;
                case "43"://LW
                    currentInstructionOpType = InstructionType.I;
                    currentInstructionOp = Instruction.LW;
                    break;
                case "04"://MOV
                    currentInstructionOpType = InstructionType.R;
                    currentInstructionOp = Instruction.MOV;
                    break;
                case "05"://ADD
                    currentInstructionOpType = InstructionType.R;
                    currentInstructionOp = Instruction.ADD;
                    break;
                case "06"://SUB
                    currentInstructionOpType = InstructionType.R;
                    currentInstructionOp = Instruction.SUB;
                    break;
                case "07"://MUL
                    currentInstructionOpType = InstructionType.R;
                    currentInstructionOp = Instruction.MUL;
                    break;
                case "08"://DIV
                    currentInstructionOpType = InstructionType.R;
                    currentInstructionOp = Instruction.DIV;
                    break;
                case "09"://AND
                    currentInstructionOpType = InstructionType.R;
                    currentInstructionOp = Instruction.AND;
                    break;
                case "0A"://OR
                    currentInstructionOpType = InstructionType.R;
                    currentInstructionOp = Instruction.OR;
                    break;
                case "4B"://MOVI
                    currentInstructionOpType = InstructionType.I;
                    currentInstructionOp = Instruction.MOVI;
                    break;
                case "4C"://ADDI
                    currentInstructionOpType = InstructionType.I;
                    currentInstructionOp = Instruction.ADDI;
                    break;
                case "4D"://MULI
                    currentInstructionOpType = InstructionType.I;
                    currentInstructionOp = Instruction.MULI;
                    break;
                case "4E"://DIVI
                    currentInstructionOpType = InstructionType.I;
                    currentInstructionOp = Instruction.DIVI;
                    break;
                case "4F"://LDI
                    currentInstructionOpType = InstructionType.I;
                    currentInstructionOp = Instruction.LDI;
                    break;
                case "10"://SLT
                    currentInstructionOpType = InstructionType.R;
                    currentInstructionOp = Instruction.SLT;
                    break;
                case "51"://SLTI
                    currentInstructionOpType = InstructionType.I;
                    currentInstructionOp = Instruction.SLTI;
                    break;
                case "92"://HLT
                    currentInstructionOpType = InstructionType.J;
                    currentInstructionOp = Instruction.HLT;
                    break;
                case "13"://NOP
                    currentInstructionOpType = InstructionType.NOP;
                    currentInstructionOp = Instruction.NOP;
                    break;
                case "94"://JMP
                    currentInstructionOpType = InstructionType.J;
                    currentInstructionOp = Instruction.JMP;
                    break;
                case "55"://BEQ
                    currentInstructionOpType = InstructionType.I;
                    currentInstructionOp = Instruction.BEQ;
                    break;
                case "56"://BNE
                    currentInstructionOpType = InstructionType.I;
                    currentInstructionOp = Instruction.BNE;
                    break;
                case "57"://BEZ
                    currentInstructionOpType = InstructionType.I;
                    currentInstructionOp = Instruction.BEZ;
                    break;
                case "58"://BNZ
                    currentInstructionOpType = InstructionType.I;
                    currentInstructionOp = Instruction.BNZ;
                    break;
                case "59"://BGZ
                    currentInstructionOpType = InstructionType.I;
                    currentInstructionOp = Instruction.BGZ;
                    break;
                case "5A"://BLZ
                    currentInstructionOpType = InstructionType.I;
                    currentInstructionOp = Instruction.BLZ;
                    break;
                default:
                    Console.WriteLine("oops!");
                    break;
            }
            #endregion
            #region Reg Switch
            switch (currentInstructionOpType)
            {
                case InstructionType.R:
                    cpuPCB.reg1 = SystemCaller.ConvertHexstringToUInt(currentInstructionCopy.Substring(0, 1));
                    cpuPCB.reg2 = SystemCaller.ConvertHexstringToUInt(currentInstructionCopy.Substring(1, 1));
                    cpuPCB.dreg = SystemCaller.ConvertHexstringToUInt(currentInstructionCopy.Substring(2, 1));
                    break;
                case InstructionType.IO:
                    cpuPCB.IoCount++;
                    if (currentInstructionOp == Instruction.RD)
                    {
                        cpuPCB.reg1 = SystemCaller.ConvertHexstringToUInt(currentInstructionCopy.Substring(0, 1));
                        cpuPCB.reg2 = SystemCaller.ConvertHexstringToUInt(currentInstructionCopy.Substring(1, 1));
                        cpuPCB.address = SystemCaller.ConvertHexstringToUInt(currentInstructionCopy.Substring(2, 4)) / 4;
                        //cpuPCB.address += cpuPCB.SeparationOffset;
                    }
                    else if (currentInstructionOp == Instruction.WR)
                    {
                        cpuPCB.reg1 = SystemCaller.ConvertHexstringToUInt(currentInstructionCopy.Substring(0, 1));
                        cpuPCB.reg2 = SystemCaller.ConvertHexstringToUInt(currentInstructionCopy.Substring(1, 1));
                        cpuPCB.address = SystemCaller.ConvertHexstringToUInt(currentInstructionCopy.Substring(2, 4)) / 4;
                        //cpuPCB.address += cpuPCB.SeparationOffset;
                    }
                    break;
                case InstructionType.I:
                    cpuPCB.breg = SystemCaller.ConvertHexstringToUInt(currentInstructionCopy.Substring(0, 1));
                    cpuPCB.dreg = SystemCaller.ConvertHexstringToUInt(currentInstructionCopy.Substring(1, 1));
                    cpuPCB.address = SystemCaller.ConvertHexstringToUInt(currentInstructionCopy.Substring(2, 4));
                    //cpuPCB.address += cpuPCB.SeparationOffset;
                    break;
                case InstructionType.J:
                    cpuPCB.address = SystemCaller.ConvertHexstringToUInt(currentInstructionCopy.Substring(0, 6)) / 4;
                    //cpuPCB.address += cpuPCB.SeparationOffset;
                    break;
                case InstructionType.NOP:
                    break;
            }
            #endregion
        }

        void Execute()
        {
            switch (currentInstructionOp)
            {
                case Instruction.RD:
                    // Reads the content of IP buffer into a accumulator.
                    //Console.Write("Reading ");
                    if (cpuPCB.reg2 == 0)
                    {
                        //Console.WriteLine("data ["+cache[cpuPCB.address].ToString()+"] from cpuPCB.address ["+cpuPCB.address.ToString()+"] to Register " + cpuPCB.reg1 + ".");
                        //cpuPCB.register[cpuPCB.reg1].WriteToRegister(cache[cpuPCB.address]);
                        cpuPCB.register[cpuPCB.reg1].WriteToRegister(ReadFromCache(cpuPCB.address));
                    }
                    else
                    {
                        //Console.WriteLine("data [" + cpuPCB.register[cpuPCB.reg2].ReadData() + "] from Register "+cpuPCB.reg2+" to Register "+cpuPCB.reg1+".");
                        //cpuPCB.register[cpuPCB.reg1].WriteToRegister(cache[cpuPCB.register[cpuPCB.reg2].ReadData()]);
                        cpuPCB.register[cpuPCB.reg1].WriteToRegister(ReadFromCache(cpuPCB.register[cpuPCB.reg2].ReadData()));
                    }
                    //Console.WriteLine("Completed: Register "+cpuPCB.reg1+" now contains " + cpuPCB.register[cpuPCB.reg1].ReadData().ToString() +
                        //" and Register "+cpuPCB.reg2+" now contains " + cpuPCB.register[cpuPCB.reg2].ReadData().ToString());
                    break;
                case Instruction.WR:
                    // Writes the content of the accumulator into the OP buffer
                    //Console.WriteLine("Writing ");
                    if (cpuPCB.address == 0)
                    {
                        //Console.WriteLine("Register 1 is Register " + cpuPCB.reg1 + " and Register 2 is " + cpuPCB.reg2 + ".");
                        //Console.WriteLine("Register " + cpuPCB.reg1 + " contains " + cpuPCB.register[cpuPCB.reg1].ReadData());
                        //Console.WriteLine("Register " + cpuPCB.reg2 + " contains " + cpuPCB.register[cpuPCB.reg2].ReadData());
                        //Console.WriteLine("data [" + cpuPCB.register[cpuPCB.reg1].ReadData() + "] from Register "+cpuPCB.reg1+" to Register "+cpuPCB.reg2+".");
                        //cache[cpuPCB.register[cpuPCB.reg2].ReadData()] = cpuPCB.register[cpuPCB.reg1].ReadData();
                        WriteToCache(cpuPCB.register[cpuPCB.reg1].ReadData(), cpuPCB.register[cpuPCB.reg2].ReadData());
                        //Console.WriteLine(cache[cpuPCB.register[cpuPCB.reg2].ReadData()]);
                    }
                    else
                    {
                        //Console.WriteLine("data [" + cpuPCB.register[cpuPCB.reg1].ReadData() + "] from Register 1 to cpuPCB.address [" + cpuPCB.address + "].");
                        //cache[cpuPCB.address] = cpuPCB.register[cpuPCB.reg1].ReadData();
                        WriteToCache(cpuPCB.register[cpuPCB.reg1].ReadData(), cpuPCB.address);
                    }
                    //Console.WriteLine("Completed.");
                    break;
                case Instruction.ST: // ***
                    // Stores the content of a register into an cpuPCB.address
                    //Console.WriteLine("Storing content of a register into an cpuPCB.address/register.");
                    if (cpuPCB.dreg == 0)
                    {
                    //    Console.WriteLine("Writing data " + cache[cpuPCB.address] + " from cpuPCB.address " + cpuPCB.address + ".");
                        //cpuPCB.register[cpuPCB.dreg].WriteToRegister(cache[cpuPCB.address]);
                        cpuPCB.register[cpuPCB.dreg].WriteToRegister(ReadFromCache(cpuPCB.address));
                        //Console.WriteLine("Completed. Destination register now contains " + cpuPCB.register[cpuPCB.dreg].ReadData() + ".");
                    }
                    else
                    {
                        //Console.WriteLine("Register " + cpuPCB.breg + " contains " + cpuPCB.register[cpuPCB.breg].ReadData() + ".");
                        //Console.WriteLine("Writing to the cpuPCB.address pointed to by Register " + cpuPCB.dreg + " from Register " + cpuPCB.breg + ".");
                        //cache[cpuPCB.register[cpuPCB.dreg].ReadData()] = cpuPCB.register[cpuPCB.breg].ReadData();
                        WriteToCache(cpuPCB.register[cpuPCB.breg].ReadData(), cpuPCB.register[cpuPCB.dreg].ReadData());
                        //Console.WriteLine("cpuPCB.address location " + cpuPCB.register[cpuPCB.dreg].ReadData() + " is now " + cache[cpuPCB.register[cpuPCB.dreg].ReadData()] + ".");
                    }
                    break;
                case Instruction.LW:
                    // Loads the content of an cpuPCB.address into a register
                    //Console.WriteLine("Writing contents of cpuPCB.address " + cpuPCB.register[9].ReadData() + " to register " + cpuPCB.dreg);
                    //cpuPCB.register[cpuPCB.dreg].WriteToRegister(cache[cpuPCB.register[cpuPCB.breg].ReadData() + cpuPCB.address]);
                    cpuPCB.register[cpuPCB.dreg].WriteToRegister(ReadFromCache(cpuPCB.register[cpuPCB.breg].ReadData() + cpuPCB.address));
                    //Console.WriteLine("Register " + cpuPCB.dreg + " is now " + cpuPCB.register[cpuPCB.dreg].ReadData());
                    break;
                case Instruction.MOV:
                    // Transfers the content of one register into another
                    //Console.WriteLine("Register " + cpuPCB.scpuPCB.reg1 + " is receiving data from Register " + cpuPCB.scpuPCB.reg2 + ".");
                    cpuPCB.register[cpuPCB.reg1].WriteToRegister(cpuPCB.register[cpuPCB.reg2].ReadData());
                    //Console.WriteLine(cpuPCB.register[cpuPCB.scpuPCB.reg1].ReadData());
                    break;
                case Instruction.ADD://ADD
                    // Adds the contents of two Sregs into cpuPCB.dreg
                    //Console.WriteLine("S-register 1 is register " + cpuPCB.scpuPCB.reg1 + " and S-register 2 is register " + cpuPCB.scpuPCB.reg2 + " and D-register is register " + cpuPCB.dreg + ".");
                    cpuPCB.register[cpuPCB.dreg].WriteToRegister(cpuPCB.register[cpuPCB.reg1].ReadData() + cpuPCB.register[cpuPCB.reg2].ReadData());
                    //Console.WriteLine("Completed. Register " + cpuPCB.dreg + " is " + cpuPCB.register[cpuPCB.dreg].ReadData() + ".");
                    break;
                case Instruction.SUB://SUB
                    // Subtracts the contents of two Sregs into cpuPCB.dreg
                    cpuPCB.register[cpuPCB.dreg].WriteToRegister(cpuPCB.register[cpuPCB.reg1].ReadData() - cpuPCB.register[cpuPCB.reg2].ReadData());
                    break;
                case Instruction.MUL:
                    // Multiplies the contents of two Sregs into cpuPCB.dreg
                    cpuPCB.register[cpuPCB.dreg].WriteToRegister(cpuPCB.register[cpuPCB.reg1].ReadData() * cpuPCB.register[cpuPCB.reg2].ReadData());
                    break;
                case Instruction.DIV:
                    // Divides the contents of two Sregs into cpuPCB.dreg
                    cpuPCB.register[cpuPCB.dreg].WriteToRegister(cpuPCB.register[cpuPCB.reg1].ReadData() / cpuPCB.register[cpuPCB.reg2].ReadData());
                    break;
                case Instruction.AND:
                    // Logical AND of two Sregs into cpuPCB.dreg
                    cpuPCB.register[cpuPCB.dreg].WriteToRegister(cpuPCB.register[cpuPCB.reg1].ReadData() & cpuPCB.register[cpuPCB.reg2].ReadData());
                    break;
                case Instruction.OR:
                    // Logical OR of two Sregs into cpuPCB.dreg
                    cpuPCB.register[cpuPCB.dreg].WriteToRegister(cpuPCB.register[cpuPCB.reg1].ReadData() | cpuPCB.register[cpuPCB.reg2].ReadData());
                    break;
                case Instruction.MOVI:
                    // Transfers an cpuPCB.address/data directly into a register
                    //Console.WriteLine("Moving cpuPCB.address ["+cpuPCB.address+"] into Destination Register ["+cpuPCB.dreg+"].");
                    cpuPCB.register[cpuPCB.dreg].WriteToRegister(cpuPCB.address);
                    //Console.WriteLine("Completed. Destination register now contains [" + cpuPCB.register[cpuPCB.dreg].ReadData() + "].");
                    break;
                case Instruction.ADDI:
                    // Adds data directly to the content of a register
                    //Console.WriteLine("Adding data or cpuPCB.addressing directly to the register.");
                    // Handle increment by 1.
                    if (cpuPCB.address == 1)
                    {
                        //Console.WriteLine("cpuPCB.address == 1, using as data for incrementation.");
                        cpuPCB.register[cpuPCB.dreg].WriteToRegister(cpuPCB.register[cpuPCB.dreg].ReadData() + 1);
                        //Console.WriteLine("D-reg " + cpuPCB.dreg + " is now " + cpuPCB.register[cpuPCB.dreg].ReadData() + ".");
                    }
                    // Handle incrementation by cpuPCB.addressing.
                    else
                    {
                        //Console.WriteLine("cpuPCB.address is not one, using cpuPCB.addressing. Giving " + cpuPCB.address + " to Destination register " + cpuPCB.dreg + ".");
                        cpuPCB.register[cpuPCB.dreg].WriteToRegister(cpuPCB.register[cpuPCB.dreg].ReadData() + (cpuPCB.address/4));
                        //Console.WriteLine("Completed. Destination register now contains the cpuPCB.address " + cpuPCB.register[cpuPCB.dreg].ReadData() + ".");
                    }
                    break;
                case Instruction.MULI:
                    // Multiplies data directly to the content of a register
                    // Assuming cpuPCB.address will always be data.
                    cpuPCB.register[cpuPCB.dreg].WriteToRegister(cpuPCB.register[cpuPCB.dreg].ReadData() * cpuPCB.address);
                    break;
                case Instruction.DIVI:
                    // Divides data directly to the content of a register
                    // Assuming cpuPCB.address will always be data.
                    cpuPCB.register[cpuPCB.dreg].WriteToRegister(cpuPCB.register[cpuPCB.dreg].ReadData() / cpuPCB.address);
                    break;
                case Instruction.LDI:
                    // Loads data/cpuPCB.address directly to the content of a register
                    //Console.WriteLine("Loading cpuPCB.address " + cpuPCB.address/4 + " into Destination Register " + cpuPCB.dreg + ".");
                    cpuPCB.register[cpuPCB.dreg].WriteToRegister(cpuPCB.address/4);
                    //Console.WriteLine("Completed. Destination register is now " + cpuPCB.register[cpuPCB.dreg].ReadData() + ".");
                    break;
                case Instruction.SLT:
                    // Sets the D-reg to 1 if first cpuPCB.scpuPCB.reg1 is less than second cpuPCB.scpuPCB.reg2, 0 otherwise
                    //Console.WriteLine("Checking S-reg "+cpuPCB.scpuPCB.reg1+" < S-reg "+cpuPCB.scpuPCB.reg2+".");
                    //Console.WriteLine(cpuPCB.register[cpuPCB.scpuPCB.reg1].ReadData() + " < " + cpuPCB.register[cpuPCB.scpuPCB.reg2].ReadData());
                    //Console.WriteLine("Destination register is register " + cpuPCB.dreg);
                    if (cpuPCB.register[cpuPCB.reg1].ReadData() < cpuPCB.register[cpuPCB.reg2].ReadData())
                    {
                        //Console.WriteLine("True. Writing 1 to Destination Register " + cpuPCB.dreg + ".");
                        cpuPCB.register[cpuPCB.dreg].WriteToRegister(1); 
                    }
                    else
                    {
                        //Console.WriteLine("False. Writing 0 to Destination Register " + cpuPCB.dreg + ".");
                        cpuPCB.register[cpuPCB.dreg].WriteToRegister(0);
                    }
                    //Console.WriteLine("Completed.");
                    break;
                case Instruction.SLTI:
                    // Sets the D-reg to 1 if first Sreg is less than data, 0 otherwise
                    if (cpuPCB.breg < cpuPCB.address) cpuPCB.register[cpuPCB.dreg].WriteToRegister(1);
                    else cpuPCB.register[cpuPCB.dreg].WriteToRegister(0);
                    break;
                case Instruction.HLT:
                    // Logical end of program
                    //Console.WriteLine("Register 0 holds " + cpuPCB.register[0].ReadData() + ".");
                    //Console.WriteLine("Output buffer is "+cpuPCB.OutputBuffer+" and holds " + cache[48] + ".");
                    //for (uint i = 0; i < cache.GetLength(0); i++)
                    //{
                    //    // Reading contents of memory.
                    //    Console.WriteLine(cache[i]);
                    //}
                    ProcessFinished();
                    break;
                case Instruction.NOP:
                    // Does nothing - moves to next instruction
                    break;
                case Instruction.JMP:
                    // Jumps to a specified location
                    cpuPCB.ProgramCounter = cpuPCB.address;
                    break;
                case Instruction.BEQ:
                    // Branches to an cpuPCB.address when the content of cpuPCB.breg = cpuPCB.dreg
                    //Console.WriteLine("Checking B-reg " + cpuPCB.breg + " != D-reg " + cpuPCB.dreg + ".");
                    if (cpuPCB.register[cpuPCB.breg].ReadData() == cpuPCB.register[cpuPCB.dreg].ReadData())
                    {
                        //Console.WriteLine("True. Jumping to cpuPCB.address " + cpuPCB.address + ".");
                        cpuPCB.ProgramCounter = cpuPCB.address/4;
                    }
                    else
                    {
                        //Console.WriteLine("False. Continuing on. Current PC is " + cpuPCB.ProgramCounter);
                    }
                    //Console.WriteLine("Completed.");
                    break;
                case Instruction.BNE:
                    // Branches to an cpuPCB.address when the content of cpuPCB.breg <> cpuPCB.dreg
                    //Console.WriteLine("Checking B-reg " + cpuPCB.breg + " != D-reg " + cpuPCB.dreg + ".");
                    if (cpuPCB.register[cpuPCB.breg].ReadData() != cpuPCB.register[cpuPCB.dreg].ReadData())
                    {
                        //Console.WriteLine("True. Jumping to cpuPCB.address " + cpuPCB.address + ".");
                        cpuPCB.ProgramCounter = cpuPCB.address/4;
                    }
                    else
                    {
                        //Console.WriteLine("False. Continuing on. Current PC is " + cpuPCB.ProgramCounter);
                    }
                    //Console.WriteLine("Completed.");
                    break;
                case Instruction.BEZ:
                    // Branches to an cpuPCB.address when the content of cpuPCB.dreg = 0
                    if (cpuPCB.register[cpuPCB.dreg].ReadData() == 0) cpuPCB.ProgramCounter = cpuPCB.address;
                    break;
                case Instruction.BNZ:
                    // Branches to an cpuPCB.address when the content of cpuPCB.breg <> 0
                    if (cpuPCB.register[cpuPCB.breg].ReadData() != 0) cpuPCB.ProgramCounter = cpuPCB.address;
                    break;
                case Instruction.BGZ:
                    // Branches to an cpuPCB.address when the content of cpuPCB.breg > 0
                    if (cpuPCB.register[cpuPCB.breg].ReadData() > 0) cpuPCB.ProgramCounter = cpuPCB.address;
                    break;
                case Instruction.BLZ:
                    // Branches to an cpuPCB.address when the content of cpuPCB.breg < 0
                    if (cpuPCB.register[cpuPCB.breg].ReadData() < 0) cpuPCB.ProgramCounter = cpuPCB.address;
                    break;
                default:
                    Console.WriteLine("OOPS!");
                    break;
            }
        }

        void SaveProcessStatus()
        {
            // Save process's PCB as CPU's PCB.
            currentProcess.PCB = cpuPCB;
        }

        void ProcessFinished()
        {
            totalElapsedTime.Stop();
            cpuPCB.completionTime = totalElapsedTime.Elapsed.TotalMilliseconds;
            SaveProcessStatus();
            kernel.deadProcesses.Add(currentProcess);
            Console.WriteLine("Process " + cpuPCB.ProcessID + " used " + cpuPCB.IoCount + " I/O calls.");
            Console.WriteLine("Elapsed time for CPU " + id + " to run job " + cpuPCB.ProcessID + " was " + totalElapsedTime.Elapsed.TotalMilliseconds.ToString() + "ms.\n");
            currentProcess = null;
        }

        public bool HasProcess()
        {
            if (currentProcess == null) return false;
            return true;
        }

        #region The True Demon's Souls Begins Here

        void WriteToCache(uint data, uint address)
        {
            uint actualPage = cpuPCB.PageTable.ReturnFirstPage(cpuPCB) + (address / 4);
            uint frame = cpuPCB.PageTable.Lookup(actualPage).Frame;
            if (address < cpuPCB.InstructionLength) // if instruction
            { 
                // shouldn't write to instruction cache
                Console.WriteLine("You're fking up!");
            }
            address += cpuPCB.SeparationOffset;
            if (address + cpuPCB.SeparationOffset < cpuPCB.InstructionLength + cpuPCB.InputBufferSize + cpuPCB.SeparationOffset) // if data
            {
                // don't write to data cache
                Console.WriteLine("You're fking up!");
            }
            else if (address + cpuPCB.SeparationOffset < cpuPCB.InstructionLength + cpuPCB.InputBufferSize + cpuPCB.OutputBufferSize + cpuPCB.SeparationOffset) // if output
            {
                // need to check if page frame is in cache.
                //
                if (cpuPCB.Cache_Output.HasFrame(frame)) // checking to see if it's in cache
                {
                    // if it's in cache, write to it
                    cpuPCB.Cache_Output.Write(data, address + cpuPCB.SeparationOffset);
                }
                // not in cache, check to see if page is in memory
                if (cpuPCB.PageTable.Lookup(actualPage).InMemory)
                {
                    // in memory
                    if (cpuPCB.Cache_Output.GetCorrespondingFrame(cpuPCB.Cache_Output.CurrentCacheIndex) != 257)
                    {
                        MMU.WriteFrame(cpuPCB.Cache_Output.ReadByCache(cpuPCB.Cache_Output.CurrentCacheIndex), cpuPCB.Cache_Output.GetFrame(cpuPCB.Cache_Output.CurrentCacheIndex));
                    }
                    cpuPCB.Cache_Output.MapFrame(cpuPCB.Cache_Output.CurrentCacheIndex, frame);
                    cpuPCB.Cache_Output.Write(MMU.ReadFrame(frame), cpuPCB.Cache_Output.NextFrame());
                    cpuPCB.Cache_Output.Write(data, address + cpuPCB.SeparationOffset);
                }
                // not in memory
                PageFault(actualPage);
            }
            else // temp AKA no cache
            {
                MMU.Write(address + cpuPCB.SeparationOffset, data);
            }
        }

        uint ReadFromCache(uint address)
        {
            uint actualPage = cpuPCB.PageTable.ReturnFirstPage(cpuPCB) + (address / 4);
            uint frame = cpuPCB.PageTable.Lookup(actualPage).Frame;
            if (address < cpuPCB.InstructionLength) // if instruction
            {
                // need to check if page frame is in cache.
                //
                if (cpuPCB.Cache_Instruction.HasFrame(frame)) // checking to see if it's in cache
                {
                    // if it's in cache, write to it
                    return cpuPCB.Cache_Instruction.ReadByAddress(address);
                }
                // not in cache, check to see if page is in memory
                if (cpuPCB.PageTable.Lookup(actualPage).InMemory)
                {
                    // in memory -- cpuPCB.Cache_Instruction.GetCorrespondingFrame(cpuPCB.Cache_Instruction.CurrentCacheIndex) != 257)
                    if (cpuPCB.Cache_Instruction.GetCorrespondingFrame(cpuPCB.Cache_Instruction.CurrentCacheIndex) != 257)
                    {
                        MMU.WriteFrame(cpuPCB.Cache_Instruction.ReadByCache(cpuPCB.Cache_Instruction.CurrentCacheIndex), cpuPCB.Cache_Instruction.GetFrame(cpuPCB.Cache_Instruction.CurrentCacheIndex));
                    }
                    cpuPCB.Cache_Instruction.MapFrame(cpuPCB.Cache_Instruction.CurrentCacheIndex, frame);
                    cpuPCB.Cache_Instruction.Write(MMU.ReadFrame(frame), cpuPCB.Cache_Instruction.NextFrame());
                    return cpuPCB.Cache_Instruction.ReadByAddress(address);
                }
                // not in memory
                PageFault(actualPage);
                return 0;
            }
            if (address + cpuPCB.SeparationOffset < cpuPCB.InstructionLength + cpuPCB.InputBufferSize + cpuPCB.SeparationOffset) // if data
            {
                // need to check if page frame is in cache.
                //
                if (cpuPCB.Cache_Data.HasFrame(frame)) // checking to see if it's in cache
                {
                    // if it's in cache, write to it
                    return cpuPCB.Cache_Data.ReadByAddress(address + cpuPCB.SeparationOffset);
                }
                // not in cache, check to see if page is in memory
                if (cpuPCB.PageTable.Lookup(actualPage).InMemory)
                {
                    // in memory
                    if (cpuPCB.Cache_Data.GetCorrespondingFrame(cpuPCB.Cache_Data.CurrentCacheIndex) != 257)
                    {
                        MMU.WriteFrame(cpuPCB.Cache_Data.ReadByCache(cpuPCB.Cache_Data.CurrentCacheIndex), cpuPCB.Cache_Data.GetFrame(cpuPCB.Cache_Data.CurrentCacheIndex));
                    }
                    cpuPCB.Cache_Data.MapFrame(cpuPCB.Cache_Data.CurrentCacheIndex, frame);
                    cpuPCB.Cache_Data.Write(MMU.ReadFrame(frame), cpuPCB.Cache_Data.NextFrame());
                    return cpuPCB.Cache_Data.ReadByAddress(address + cpuPCB.SeparationOffset);
                }
                // not in memory
                PageFault(actualPage);
                return 0;
            }
            else if (address + cpuPCB.SeparationOffset < cpuPCB.InstructionLength + cpuPCB.InputBufferSize + cpuPCB.OutputBufferSize + cpuPCB.SeparationOffset) // if output
            {
                // need to check if page frame is in cache.
                //
                if (cpuPCB.Cache_Output.HasFrame(frame)) // checking to see if it's in cache
                {
                    // if it's in cache, write to it
                    return cpuPCB.Cache_Output.ReadByAddress(address + cpuPCB.SeparationOffset);
                }
                // not in cache, check to see if page is in memory
                if (cpuPCB.PageTable.Lookup(actualPage).InMemory)
                {
                    // in memory
                    if (cpuPCB.Cache_Output.GetCorrespondingFrame(cpuPCB.Cache_Output.CurrentCacheIndex) != 257)
                    {
                        MMU.WriteFrame(cpuPCB.Cache_Output.ReadByCache(cpuPCB.Cache_Output.CurrentCacheIndex), cpuPCB.Cache_Output.GetFrame(cpuPCB.Cache_Output.CurrentCacheIndex));
                    }
                    cpuPCB.Cache_Output.MapFrame(cpuPCB.Cache_Output.CurrentCacheIndex, frame);
                    cpuPCB.Cache_Output.Write(MMU.ReadFrame(frame), cpuPCB.Cache_Output.NextFrame());
                    return cpuPCB.Cache_Output.ReadByAddress(address + cpuPCB.SeparationOffset);
                }
                // not in memory
                PageFault(actualPage);
                return 0;
            }
            else // temp AKA no cache
            {
                return MMU.Read(address + cpuPCB.SeparationOffset);
            }
        }

        void PageFault(uint page)
        {
            cpuPCB.ProgramCounter--;
            cpuPCB.Interrupt = Interrupt.PageFault;
            SaveProcessStatus();
            InterruptHandler.EnqueueProcess(currentProcess, page);
            currentProcess = null;
        }
        #endregion
    }
}
