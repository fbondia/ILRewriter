using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ILRewriterAttributes
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false)]
    public class PropertyLogging : Attribute
    {
        public static void Get(string propertyName, ref object value)
        {
            Console.WriteLine(string.Format("Get property with name '{0}' and value '{1}'", propertyName, value.ToString()));
        }

        public static void Set(string propertyName, ref object value)
        {
            Console.WriteLine(string.Format("Set property with name '{0}' and value '{1}'", propertyName, value.ToString()));
        }
    }

    [AttributeUsage(AttributeTargets.Property, Inherited = false)]
    public class PropertyNotNull : Attribute
    {
        public static void Get(string propertyName, ref object value)
        {
            if(value == null)
            {
                throw new ArgumentNullException(propertyName, string.Format("Property '{0}' Method 'Get' is null.", propertyName));
            }
        }

        public static void Set(string propertyName, ref object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(propertyName, string.Format("Property '{0}' Method 'Set' is null.", propertyName));
            }
        }
    }
}
