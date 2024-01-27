using System;
using System.Collections.Generic;
using System.Text;

namespace OriBot.Shared
{
    public abstract class Inherited2 : IAssemblyEntryPoint
    {
        public void CalledByCommand()
        {
            Console.WriteLine("123");
        }
        public abstract void Dispose();
    }
}
