using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OS_PROJECT
{
    enum RegisterType { Accumulator, Zero, GeneralPurpose }

    class Register
    {
        RegisterType registerType;
        protected uint registerMemoryData;

        public Register()
        { }

        public Register(RegisterType regType)
        {
            RegisterType = regType;
        }

        public void WriteToRegister(uint data)
        {
            if (registerType != RegisterType.Zero)
            {
                registerMemoryData = data;
            }
            else
            {
                //Console.WriteLine("Could not write to register-- Attempted to write to Zero Register.");
            }
        }

        public uint ReadData()
        {
            return registerMemoryData;
        }

        public RegisterType RegisterType
        { get { return registerType; } set { registerType = value; } }
    }
}
