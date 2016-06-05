using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ILRewriterAttributes
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class MethodLogging : Attribute
    {
        Stopwatch stopWatch;

        public void PreMethod(string name, params object[] arguments)
        {
            Console.WriteLine(string.Format("{0} Enter method: '{1}' Parameter: '{2}'", DateTime.Now, name, string.Join(", ", arguments)));
            stopWatch = new Stopwatch();
            stopWatch.Start();
        }
        public void PostMethod(string name, params object[] arguments)
        {
            stopWatch.Stop();
            Console.WriteLine(string.Format("{0} Leaving method: '{1}' Parameter: '{2}' Duration: '{3} ms'", DateTime.Now, name, string.Join(", ", arguments), stopWatch.ElapsedMilliseconds));
        }
        
    }

    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class MethodWorktimeCall : Attribute
    {
        Stopwatch stopWatch;

        public void PreMethod(string name, params object[] arguments)
        {
            if(DateTime.Now.Hour < 9 || DateTime.Now.Hour > 17)
            {
                throw new InvalidOperationException(string.Format("The method '{0}' is only callabel between 9 to 5.", name));
            }
        }
        public void PostMethod(string name, params object[] arguments)
        {

        }
    }
}
