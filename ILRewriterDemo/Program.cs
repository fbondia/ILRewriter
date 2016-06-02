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
            DoSomething("einText", 4);

            Console.ReadLine();
        }

        [MethodLogging]
        static void DoSomething(string text, int tt)
        {
            Console.WriteLine("DoSomething() method body. Text: " + text + " Zahl: "+tt.ToString());
        }
    }
}
