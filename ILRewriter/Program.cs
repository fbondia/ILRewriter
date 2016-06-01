using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ILRewriter
{
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length == 0)
            {
                Console.WriteLine("Please supply the assembly to rewrite as first parameter.");
                Console.WriteLine("e.g ILRewriter.exe C:\assembly.dll");
                return;
            }

            var rewriter = new ILCodeRewriter(args[0]);

            rewriter.RewriteMethods();

            rewriter.Reweave();
        }
    }
}
