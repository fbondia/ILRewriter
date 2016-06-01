using ILRewriterAttributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ILRewriterDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            DoSomething();

            Console.ReadLine();
        }

        [MethodLogging]
        static void DoSomething()
        {
            Console.WriteLine("DoSomething() method body.");
        }
    }
}
