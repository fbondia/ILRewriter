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

        private string dataString = "data";
        [PropertyLogging, PropertyCall]
        public string DataString
        {
            get
            {
                return dataString;
            }
            set
            {
                dataString = value;
            }
        }

        static void Main(string[] args)
        {
            DoSomething(null, 4);

            Console.ReadLine();
        }

        [MethodLogging, MethodCall]
        static void DoSomething([NotNullOrEmpty, NotNull]string text, [NotNull]int tt)
        {
            Console.WriteLine("DoSomething() method body. Text: " + text + " Zahl: "+tt.ToString());
        }
    }
}
