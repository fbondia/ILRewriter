using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ILRewriterAttributes
{
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    public class NotNull : Attribute
    {
        public static void Process(string name, object value)
        {
            if(value == null)
            {
                throw new ArgumentNullException(name, string.Format("Parameter '{0}' is null.", name));
            }
        }
    }
}
