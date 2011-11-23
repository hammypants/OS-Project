using System;
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

    public enum CacheType
    {
        Instruction, Data, Output
    }

    public struct Cache
    {
        public uint CurrentCacheIndex;
        public CacheType Type;
        uint[][] cache;
        uint[] CacheTable; // maps cache frames to frames

        public Cache(CacheType type)
        {
            CurrentCacheIndex = 0;
            cache = new uint[4][];
            CacheTable = new uint[4];
            Type = type;
            Initialize();
        }

        void Initialize()
        {
            for (int i = 0; i < 4; i++)
            {
                cache[i] = new uint[4];
                CacheTable[i] = 257;
            }
        }

        #region Methods
        public uint NextFrame()
        {
            uint returnable = CurrentCacheIndex;
            CurrentCacheIndex++;
            CurrentCacheIndex %= 4;
            return returnable;
        }

        public void Write(uint data, uint cacheFrameNum, uint location)
        {
            cache[cacheFrameNum][location] = data;
        }

        public void Write(uint[] data, uint cacheFrameNum)
        {
            cache[cacheFrameNum] = data;
        }

        public void Write(uint data, uint address)
        {
            uint page, offset;
            page = address / 4;
            offset = address % 4;
            cache[address][offset] = data;
        }

        public uint Read(uint cacheFrameNum, uint location)
        {
            return cache[cacheFrameNum][location];
        }

        public uint[] ReadByCache(uint cacheFrameNum)
        {
            return cache[cacheFrameNum];
        }

        public uint ReadByAddress(uint address)
        {
            uint page, offset, cframe = 0;
            page = address / 4;
            page = MMU.
            offset = address % 4;
            for (uint i = 0; i < 4; i++)
            {
                if (CacheTable[i] == page)
                    cframe = i;
            }
            return cache[cframe][offset];
        }

        public void MapFrame(uint cacheFrame, uint frameNum)
        {
            CacheTable[cacheFrame] = frameNum;
        }

        public uint GetFrame(uint cacheFrame)
        {
            return CacheTable[cacheFrame];
        }

        public bool HasFrame(uint frame)
        {
            for (uint i = 0; i < 4; i++)
            {
                if (CacheTable[i] == frame && frame != 257)
                    return true;
            }
            return false;
        }

        public uint GetCorrespondingFrame(uint cacheIndex)
        {
            return CacheTable[cacheIndex];
        }
        #endregion
    }

    class PCB
    {
        public Register[] register = new Register[16];

        public PageTable PageTable = new PageTable();

        public Cache Cache_Instruction = new Cache(CacheType.Instruction);
        public Cache Cache_Data = new Cache(CacheType.Data);
        public Cache Cache_Output = new Cache(CacheType.Output);

        public Interrupt Interrupt;

        public uint sreg1, sreg2, dreg, reg1, reg2, breg, address = 0;

        public ExecutionPhase CurrentExecutionPhase = ExecutionPhase.Fetch;

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
