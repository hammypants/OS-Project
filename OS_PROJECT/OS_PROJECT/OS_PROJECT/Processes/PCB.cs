﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace OS_PROJECT
{
    enum ProcessState
    {
        New, Waiting, Ready, Running, Blocked, Terminated
    }

    enum ExecutionPhase
    {
        Fetch, Decode, Execute
    }

    class PCB
    {
        public Register[] register = new Register[16];

        public PageTable PageTable = new PageTable();

        public uint sreg1, sreg2, dreg, reg1, reg2, breg, address;

        public Cache Output = new Cache();
        public Cache Input = new Cache();
        public Cache Instruction = new Cache();

        public uint SeparationOffset;

        ProcessState processState = ProcessState.New;
        public ProcessState ProcessState
        { get { return processState; } set { processState = value; } }

        public Stopwatch _waitingTime = new Stopwatch();

        public double completionTime;

        public double waitingTime;

        uint ioCount = 0;
        public uint IoCount
        { get { return ioCount; } set { ioCount = value; } }

        uint processID = 0;
        public uint ProcessID
        {
            get
            {
                return processID;
            }
            set
            {
                processID = value;
            }
        }

        uint priority;
        public uint Priority
        { get { return priority; } set { priority = value; } }

        uint instructionLength;
        public uint InstructionLength
        { get { return instructionLength; } set { instructionLength = value; } }

        uint outputBufferSize;
        public uint OutputBufferSize
        { get { return outputBufferSize; } set { outputBufferSize = value; } }

        uint tempBufferSize;
        public uint TempBufferSize
        { get { return tempBufferSize; } set { tempBufferSize = value; } }

        uint inputBufferSize;
        public uint InputBufferSize
        { get { return inputBufferSize; } set { inputBufferSize = value; } }

        public uint DataBeginningDiskAddress
        { get { return DiskAddress + InstructionLength + SeparationOffset; } }

        public uint DataBeginningMemoryAddress
        { get { return MemoryAddress + InstructionLength + SeparationOffset; } }

        public uint JobLength
        { get { return InstructionLength + InputBufferSize + OutputBufferSize + TempBufferSize + SeparationOffset; } }

        uint programCounter = 0;
        public uint ProgramCounter
        { get { return programCounter; } set { programCounter = value; } }

        uint diskStartAddress;
        public uint DiskAddress
        { get { return diskStartAddress; } set { diskStartAddress = value; } }

        uint physicalAddress;
        public uint MemoryAddress
        { get { return physicalAddress; } set { physicalAddress = value; } }

        Process parent = null;
        public Process Parent
        { get { return parent; } set { parent = value; } }

        Process child = null;
        public Process Child
        { get { return child; } set { child = value; } }

        public PCB()
        {
            InstantiateRegisters();
        }

        protected void InstantiateRegisters()
        {
            for (int iterator = 0; iterator < register.GetLength(0); iterator++)
            {
                if (iterator == 0) { register[iterator] = new Register(RegisterType.Accumulator); }
                else if (iterator == 1) { register[iterator] = new Register(RegisterType.Zero); }
                else { register[iterator] = new Register(RegisterType.GeneralPurpose); }
            }
        }
    }
}
