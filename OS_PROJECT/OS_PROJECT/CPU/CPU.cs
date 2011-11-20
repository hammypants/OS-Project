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
        public struct CacheLocation
        {
            public uint[] Data;
            public uint Frame;
            public uint Page;
        }
        Process blockedProcess;
        bool faulted = false;
        bool first_page_fault_completed = false;

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
        volatile Object rwLock = new Object();

        CacheLocation[] cache_instruction = new CacheLocation[4];
        uint cache_instruction_counter = 0;
        CacheLocation[] cache_input = new CacheLocation[4];
        uint cache_input_counter = 0;
        CacheLocation[] cache_output = new CacheLocation[4];
        uint cache_output_counter = 0;
        uint[] cache = new uint[1];

        string currentInstruction;
        Instruction currentInstructionOp;
        InstructionType currentInstructionOpType;

        uint sreg1, sreg2, dreg, reg1, reg2, breg, address;

        public CPU(Driver k, int id)
        {
            kernel = k;
            disk = k.Disk;
            RAM = k.RAM;
            RQ = k.ReadyQueue;
            cpuPCB = new PCB();
            for (uint iterator = 0; iterator < 4; iterator++)
            {
                cache_instruction[iterator].Data = new uint[4];
                cache_instruction[iterator].Frame = 257;
                cache_input[iterator].Data = new uint[4];
                cache_input[iterator].Frame = 257;
                cache_output[iterator].Data = new uint[4];
                cache_output[iterator].Frame = 257;
            }
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
                    if (cpuPCB.CurrentExecutionPhase == ExecutionPhase.Fetch)
                    Fetch();
                    if (!faulted && (cpuPCB.CurrentExecutionPhase == ExecutionPhase.Fetch))
                    Decode();
                    if (!faulted && (cpuPCB.CurrentExecutionPhase == ExecutionPhase.Fetch))
                    Execute();
                }
            }
        }

        void GetProcess()
        {
            lock (dispatcherLock)
            {
                kernel.Dispatcher.DispatchProcess(this);
                if (currentProcess != null)
                {
                    cpuPCB._waitingTime.Stop();
                    cpuPCB.waitingTime = cpuPCB._waitingTime.Elapsed.TotalMilliseconds;
                    Console.WriteLine("Job " + currentProcess.PCB.ProcessID + " (Priority: " + currentProcess.PCB.Priority + " | Instr. Length: " + currentProcess.PCB.InstructionLength +
                        ") spent " + cpuPCB._waitingTime.Elapsed.TotalMilliseconds.ToString() + "ms waiting.\n");
                    processCounter++;
                    cpuPCB.CurrentExecutionPhase = ExecutionPhase.Fetch;
                    FillCache();
                    totalElapsedTime.Start();
                }
            }
        }

        void FillCache()
        {
            lock (cacheLock)
            {
                for (uint iterator = 0; iterator < 4; iterator++)
                {
                    cache_instruction[iterator].Data = MMU.ReadFrame(cpuPCB.PageTable.table[(cpuPCB.DiskAddress / 4) + iterator].Frame);
                    cache_instruction[iterator].Frame = cpuPCB.PageTable.table[(cpuPCB.DiskAddress / 4) + iterator].Frame;
                    cache_instruction[iterator].Page = cpuPCB.DiskAddress / 4 + iterator;
                }
            }
        }

        void Fetch()
        {
            if (((cpuPCB.DiskAddress + cpuPCB.ProgramCounter) / 4) == cache_instruction[0].Page)
            {
                currentInstruction = SystemCaller.ConvertInputDataToHexstring(cache_instruction[0].Data[cpuPCB.ProgramCounter % 4]);
                cpuPCB.ProgramCounter++;
                cpuPCB.CurrentExecutionPhase = ExecutionPhase.Decode;
            }
            else if (((cpuPCB.DiskAddress + cpuPCB.ProgramCounter) / 4) == cache_instruction[1].Page)
            {
                currentInstruction = SystemCaller.ConvertInputDataToHexstring(cache_instruction[1].Data[cpuPCB.ProgramCounter % 4]);
                cpuPCB.ProgramCounter++;
                cpuPCB.CurrentExecutionPhase = ExecutionPhase.Decode;
            }
            else if (((cpuPCB.DiskAddress + cpuPCB.ProgramCounter) / 4) == cache_instruction[2].Page)
            {
                currentInstruction = SystemCaller.ConvertInputDataToHexstring(cache_instruction[2].Data[cpuPCB.ProgramCounter % 4]);
                cpuPCB.ProgramCounter++;
                cpuPCB.CurrentExecutionPhase = ExecutionPhase.Decode;
            }
            else if (((cpuPCB.DiskAddress + cpuPCB.ProgramCounter) / 4) == cache_instruction[3].Page)
            {
                currentInstruction = SystemCaller.ConvertInputDataToHexstring(cache_instruction[3].Data[cpuPCB.ProgramCounter % 4]);
                cpuPCB.ProgramCounter++;
                cpuPCB.CurrentExecutionPhase = ExecutionPhase.Decode;
            }
            else
            {
                // not in cache, so check to see if it's in memory before we bring it in
                if (cpuPCB.PageTable.table[((cpuPCB.DiskAddress + cpuPCB.ProgramCounter) / 4)].InMemory)
                {
                    // in memory, load it into a cache?
                    cache_instruction[cache_instruction_counter++].Data = MMU.ReadFrame(cpuPCB.PageTable.table[((cpuPCB.DiskAddress + cpuPCB.ProgramCounter) / 4)].Frame);
                    cache_instruction_counter = cache_instruction_counter % 4;
                }
                else
                { 
                    // not in memory, page fault bitches!
                    PageFault();
                }
            }
        }

        void PageFault()
        {
            if (first_page_fault_completed == true)
            {
                Process blockedProcessToBeSwappedIn;
                ServiceBlockedProcess_PageFault();
                blockedProcessToBeSwappedIn = blockedProcess;
                blockedProcess = currentProcess;
                currentProcess = blockedProcessToBeSwappedIn;
                faulted = true;
            }
            else
            {
                blockedProcess = currentProcess;
                faulted = true;
                first_page_fault_completed = true;
            }
        }

        void ServiceBlockedProcess_PageFault()
        {
            uint frame;
            frame = MMU.GetFreeFrame(((blockedProcess.PCB.DiskAddress + blockedProcess.PCB.ProgramCounter) / 4));
            cache_instruction[cache_instruction_counter++].Data = MMU.ReadFrame(frame);
            cache_instruction_counter = cache_instruction_counter % 4;
        }

        void IOFault_Read(uint address)
        {
            currentProcess.PCB.sreg1 = cpuPCB.sreg1;
            currentProcess.PCB.sreg2 = cpuPCB.sreg2;
            currentProcess.PCB.dreg = cpuPCB.dreg;
            currentProcess.PCB.reg1 = cpuPCB.reg1;
            currentProcess.PCB.reg2 = cpuPCB.reg2;
            currentProcess.PCB.breg = cpuPCB.breg;
            currentProcess.PCB.address = cpuPCB.address;
            faulted = true;
        }

        void IOFault_Write(uint address)
        {
            currentProcess.PCB.sreg1 = cpuPCB.sreg1;
            currentProcess.PCB.sreg2 = cpuPCB.sreg2;
            currentProcess.PCB.dreg = cpuPCB.dreg;
            currentProcess.PCB.reg1 = cpuPCB.reg1;
            currentProcess.PCB.reg2 = cpuPCB.reg2;
            currentProcess.PCB.breg = cpuPCB.breg;
            currentProcess.PCB.address = cpuPCB.address;
            faulted = true;
        }

        void ServiceBlockedProcess_IOFault_Read(uint address)
        {
        }

        void ServiceBlockedProcess_IOFault_Write(uint address)
        {
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

            switch (firstTwoHexChars)
            {
                #region Switches
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
                #endregion
            }

            switch (currentInstructionOpType)
            {
                case InstructionType.R:
                    sreg1 = SystemCaller.ConvertHexstringToUInt(currentInstructionCopy.Substring(0, 1));
                    sreg2 = SystemCaller.ConvertHexstringToUInt(currentInstructionCopy.Substring(1, 1));
                    dreg = SystemCaller.ConvertHexstringToUInt(currentInstructionCopy.Substring(2, 1));
                    break;
                case InstructionType.IO:
                    cpuPCB.IoCount++;
                    if (currentInstructionOp == Instruction.RD)
                    {
                        reg1 = SystemCaller.ConvertHexstringToUInt(currentInstructionCopy.Substring(0, 1));
                        reg2 = SystemCaller.ConvertHexstringToUInt(currentInstructionCopy.Substring(1, 1));
                        address = SystemCaller.ConvertHexstringToUInt(currentInstructionCopy.Substring(2, 4)) / 4;
                        IOFault_Read(address);
                    }
                    else if (currentInstructionOp == Instruction.WR)
                    {
                        reg1 = SystemCaller.ConvertHexstringToUInt(currentInstructionCopy.Substring(0, 1));
                        reg2 = SystemCaller.ConvertHexstringToUInt(currentInstructionCopy.Substring(1, 1));
                        address = SystemCaller.ConvertHexstringToUInt(currentInstructionCopy.Substring(2, 4)) / 4;
                        IOFault_Write(address);
                    }
                    break;
                case InstructionType.I:
                    breg = SystemCaller.ConvertHexstringToUInt(currentInstructionCopy.Substring(0, 1));
                    dreg = SystemCaller.ConvertHexstringToUInt(currentInstructionCopy.Substring(1, 1));
                    address = SystemCaller.ConvertHexstringToUInt(currentInstructionCopy.Substring(2, 4));
                    break;
                case InstructionType.J:
                    address = SystemCaller.ConvertHexstringToUInt(currentInstructionCopy.Substring(0, 6)) / 4;
                    break;
                case InstructionType.NOP:
                    break;
            }
            cpuPCB.CurrentExecutionPhase = ExecutionPhase.Execute;
        }

        void Execute()
        {
            switch (currentInstructionOp)
            {
                case Instruction.RD:
                    // Reads the content of IP buffer into a accumulator.
                    //Console.Write("Reading ");
                    if (reg2 == 0)
                    {
                        //Console.WriteLine("data ["+cache[address].ToString()+"] from address ["+address.ToString()+"] to Register " + reg1 + ".");
                        cpuPCB.register[reg1].WriteToRegister(cache[address]);
                    }
                    else
                    {
                        //Console.WriteLine("data [" + cpuPCB.register[reg2].ReadData() + "] from Register "+reg2+" to Register "+reg1+".");
                        cpuPCB.register[reg1].WriteToRegister(cache[cpuPCB.register[reg2].ReadData()]);
                    }
                    //Console.WriteLine("Completed: Register "+reg1+" now contains " + cpuPCB.register[reg1].ReadData().ToString() +
                        //" and Register "+reg2+" now contains " + cpuPCB.register[reg2].ReadData().ToString());
                    break;
                case Instruction.WR:
                    // Writes the content of the accumulator into the OP buffer
                    //Console.WriteLine("Writing ");
                    if (address == 0)
                    {
                        //Console.WriteLine("Register 1 is Register " + reg1 + " and Register 2 is " + reg2 + ".");
                        //Console.WriteLine("Register " + reg1 + " contains " + cpuPCB.register[reg1].ReadData());
                        //Console.WriteLine("Register " + reg2 + " contains " + cpuPCB.register[reg2].ReadData());
                        //Console.WriteLine("data [" + cpuPCB.register[reg1].ReadData() + "] from Register "+reg1+" to Register "+reg2+".");
                        cache[cpuPCB.register[reg2].ReadData()] = cpuPCB.register[reg1].ReadData();
                        //Console.WriteLine(cache[cpuPCB.register[reg2].ReadData()]);
                    }
                    else
                    {
                        //Console.WriteLine("data [" + cpuPCB.register[reg1].ReadData() + "] from Register 1 to address [" + address + "].");
                        cache[address] = cpuPCB.register[reg1].ReadData();
                    }
                    //Console.WriteLine("Completed.");
                    break;
                case Instruction.ST: // ***
                    // Stores the content of a register into an address
                    //Console.WriteLine("Storing content of a register into an address/register.");
                    if (dreg == 0)
                    {
                    //    Console.WriteLine("Writing data " + cache[address] + " from address " + address + ".");
                        cpuPCB.register[dreg].WriteToRegister(cache[address]);
                        //Console.WriteLine("Completed. Destination register now contains " + cpuPCB.register[dreg].ReadData() + ".");
                    }
                    else
                    {
                        //Console.WriteLine("Register " + breg + " contains " + cpuPCB.register[breg].ReadData() + ".");
                        //Console.WriteLine("Writing to the address pointed to by Register " + dreg + " from Register " + breg + ".");
                        cache[cpuPCB.register[dreg].ReadData()] = cpuPCB.register[breg].ReadData();
                        //Console.WriteLine("Address location " + cpuPCB.register[dreg].ReadData() + " is now " + cache[cpuPCB.register[dreg].ReadData()] + ".");
                    }
                    break;
                case Instruction.LW:
                    // Loads the content of an address into a register
                    //Console.WriteLine("Writing contents of address " + cpuPCB.register[9].ReadData() + " to register " + dreg);
                    cpuPCB.register[dreg].WriteToRegister(cache[cpuPCB.register[breg].ReadData() + address]);
                    //Console.WriteLine("Register " + dreg + " is now " + cpuPCB.register[dreg].ReadData());
                    break;
                case Instruction.MOV:
                    // Transfers the content of one register into another
                    //Console.WriteLine("Register " + sreg1 + " is receiving data from Register " + sreg2 + ".");
                    cpuPCB.register[sreg1].WriteToRegister(cpuPCB.register[sreg2].ReadData());
                    //Console.WriteLine(cpuPCB.register[sreg1].ReadData());
                    break;
                case Instruction.ADD://ADD
                    // Adds the contents of two Sregs into Dreg
                    //Console.WriteLine("S-register 1 is register " + sreg1 + " and S-register 2 is register " + sreg2 + " and D-register is register " + dreg + ".");
                    cpuPCB.register[dreg].WriteToRegister(cpuPCB.register[sreg1].ReadData() + cpuPCB.register[sreg2].ReadData());
                    //Console.WriteLine("Completed. Register " + dreg + " is " + cpuPCB.register[dreg].ReadData() + ".");
                    break;
                case Instruction.SUB://SUB
                    // Subtracts the contents of two Sregs into Dreg
                    cpuPCB.register[dreg].WriteToRegister(cpuPCB.register[sreg1].ReadData() - cpuPCB.register[sreg2].ReadData());
                    break;
                case Instruction.MUL:
                    // Multiplies the contents of two Sregs into Dreg
                    cpuPCB.register[dreg].WriteToRegister(cpuPCB.register[sreg1].ReadData() * cpuPCB.register[sreg2].ReadData());
                    break;
                case Instruction.DIV:
                    // Divides the contents of two Sregs into Dreg
                    cpuPCB.register[dreg].WriteToRegister(cpuPCB.register[sreg1].ReadData() / cpuPCB.register[sreg2].ReadData());
                    break;
                case Instruction.AND:
                    // Logical AND of two Sregs into Dreg
                    cpuPCB.register[dreg].WriteToRegister(cpuPCB.register[sreg1].ReadData() & cpuPCB.register[sreg2].ReadData());
                    break;
                case Instruction.OR:
                    // Logical OR of two Sregs into Dreg
                    cpuPCB.register[dreg].WriteToRegister(cpuPCB.register[sreg1].ReadData() | cpuPCB.register[sreg2].ReadData());
                    break;
                case Instruction.MOVI:
                    // Transfers an address/data directly into a register
                    //Console.WriteLine("Moving address ["+address+"] into Destination Register ["+dreg+"].");
                    cpuPCB.register[dreg].WriteToRegister(address);
                    //Console.WriteLine("Completed. Destination register now contains [" + cpuPCB.register[dreg].ReadData() + "].");
                    break;
                case Instruction.ADDI:
                    // Adds data directly to the content of a register
                    //Console.WriteLine("Adding data or addressing directly to the register.");
                    // Handle increment by 1.
                    if (address == 1)
                    {
                        //Console.WriteLine("Address == 1, using as data for incrementation.");
                        cpuPCB.register[dreg].WriteToRegister(cpuPCB.register[dreg].ReadData() + 1);
                        //Console.WriteLine("D-reg " + dreg + " is now " + cpuPCB.register[dreg].ReadData() + ".");
                    }
                    // Handle incrementation by addressing.
                    else
                    {
                        //Console.WriteLine("Address is not one, using addressing. Giving " + address + " to Destination register " + dreg + ".");
                        cpuPCB.register[dreg].WriteToRegister(cpuPCB.register[dreg].ReadData() + (address/4));
                        //Console.WriteLine("Completed. Destination register now contains the address " + cpuPCB.register[dreg].ReadData() + ".");
                    }
                    break;
                case Instruction.MULI:
                    // Multiplies data directly to the content of a register
                    // Assuming address will always be data.
                    cpuPCB.register[dreg].WriteToRegister(cpuPCB.register[dreg].ReadData() * address);
                    break;
                case Instruction.DIVI:
                    // Divides data directly to the content of a register
                    // Assuming address will always be data.
                    cpuPCB.register[dreg].WriteToRegister(cpuPCB.register[dreg].ReadData() / address);
                    break;
                case Instruction.LDI:
                    // Loads data/address directly to the content of a register
                    //Console.WriteLine("Loading address " + address/4 + " into Destination Register " + dreg + ".");
                    cpuPCB.register[dreg].WriteToRegister(address/4);
                    //Console.WriteLine("Completed. Destination register is now " + cpuPCB.register[dreg].ReadData() + ".");
                    break;
                case Instruction.SLT:
                    // Sets the D-reg to 1 if first Sreg1 is less than second Sreg2, 0 otherwise
                    //Console.WriteLine("Checking S-reg "+sreg1+" < S-reg "+sreg2+".");
                    //Console.WriteLine(cpuPCB.register[sreg1].ReadData() + " < " + cpuPCB.register[sreg2].ReadData());
                    //Console.WriteLine("Destination register is register " + dreg);
                    if (cpuPCB.register[sreg1].ReadData() < cpuPCB.register[sreg2].ReadData())
                    {
                        //Console.WriteLine("True. Writing 1 to Destination Register " + dreg + ".");
                        cpuPCB.register[dreg].WriteToRegister(1); 
                    }
                    else
                    {
                        //Console.WriteLine("False. Writing 0 to Destination Register " + dreg + ".");
                        cpuPCB.register[dreg].WriteToRegister(0);
                    }
                    //Console.WriteLine("Completed.");
                    break;
                case Instruction.SLTI:
                    // Sets the D-reg to 1 if first Sreg is less than data, 0 otherwise
                    if (breg < address) cpuPCB.register[dreg].WriteToRegister(1);
                    else cpuPCB.register[dreg].WriteToRegister(0);
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
                    cpuPCB.ProgramCounter = address;
                    break;
                case Instruction.BEQ:
                    // Branches to an address when the content of Breg = Dreg
                    //Console.WriteLine("Checking B-reg " + breg + " != D-reg " + dreg + ".");
                    if (cpuPCB.register[breg].ReadData() == cpuPCB.register[dreg].ReadData())
                    {
                        //Console.WriteLine("True. Jumping to address " + address + ".");
                        cpuPCB.ProgramCounter = address/4;
                    }
                    else
                    {
                        //Console.WriteLine("False. Continuing on. Current PC is " + cpuPCB.ProgramCounter);
                    }
                    //Console.WriteLine("Completed.");
                    break;
                case Instruction.BNE:
                    // Branches to an address when the content of Breg <> Dreg
                    //Console.WriteLine("Checking B-reg " + breg + " != D-reg " + dreg + ".");
                    if (cpuPCB.register[breg].ReadData() != cpuPCB.register[dreg].ReadData())
                    {
                        //Console.WriteLine("True. Jumping to address " + address + ".");
                        cpuPCB.ProgramCounter = address/4;
                    }
                    else
                    {
                        //Console.WriteLine("False. Continuing on. Current PC is " + cpuPCB.ProgramCounter);
                    }
                    //Console.WriteLine("Completed.");
                    break;
                case Instruction.BEZ:
                    // Branches to an address when the content of Dreg = 0
                    if (cpuPCB.register[dreg].ReadData() == 0) cpuPCB.ProgramCounter = address;
                    break;
                case Instruction.BNZ:
                    // Branches to an address when the content of Breg <> 0
                    if (cpuPCB.register[breg].ReadData() != 0) cpuPCB.ProgramCounter = address;
                    break;
                case Instruction.BGZ:
                    // Branches to an address when the content of Breg > 0
                    if (cpuPCB.register[breg].ReadData() > 0) cpuPCB.ProgramCounter = address;
                    break;
                case Instruction.BLZ:
                    // Branches to an address when the content of Breg < 0
                    if (cpuPCB.register[breg].ReadData() < 0) cpuPCB.ProgramCounter = address;
                    break;
                default:
                    Console.WriteLine("OOPS!");
                    break;
            }
            cpuPCB.CurrentExecutionPhase = ExecutionPhase.Fetch;
        }

        void SaveProcessStatus()
        {
            // Save process's PCB as CPU's PCB.
            currentProcess.PCB = cpuPCB;
            // Save CPU cache into process's memory location.
            for (uint iterator = 0; iterator < cache.GetLength(0); iterator++)
            {
                RAM.WriteDataToMemory((currentProcess.PCB.MemoryAddress + iterator), cache[iterator]);
            }
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
    }
}
