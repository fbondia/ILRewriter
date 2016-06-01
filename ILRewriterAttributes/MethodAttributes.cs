using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ILRewriterAttributes
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class MethodLogging : Attribute
    {
        public static void PreMethod(string name)
        {
            Console.WriteLine(string.Format("{0} Enter method: '{1}", DateTime.Now, name));
        }
        public static void PostMethod(string name)
        {
            Console.WriteLine(string.Format("{0} Leaving method: '{1}", DateTime.Now, name));
        }
    }
}
