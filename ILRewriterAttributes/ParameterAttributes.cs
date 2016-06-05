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
        public static void Process(string methodName, string name, ref object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(name, string.Format("Method '{0}' Parameter '{1}' is null.", methodName, name));
            }
        }
    }

    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    public class NotNullOrEmpty : Attribute
    {
        public static void Process(string methodName, string name, ref object value)
        {
            if (value != null)
            {
                string data = value.ToString();
                if (!string.IsNullOrEmpty(data))
                {
                    return;
                }
            }

            throw new ArgumentNullException(name, string.Format("Method '{0}' Parameter '{1}' is null or empty.", methodName, name));
        }
    }

    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    public class GreaterThanZero : Attribute
    {
        public static void Process(string methodName, string name, ref object value)
        {
            if (value != null)
            {
                int data = Convert.ToInt32(value);
                if (data <= 0)
                {
                    throw new ArgumentNullException(name, string.Format("Method '{0}' Parameter '{1}' Value '{2}' is not greater than zero.", methodName, name, value));
                }
            }

            throw new ArgumentNullException(name, string.Format("Method '{0}' Parameter '{1}' Value '{2}' is null.", methodName, name, value));
        }
    }

    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    public class GreaterOrEqualZero : Attribute
    {
        public static void Process(string methodName, string name, ref object value)
        {
            if (value != null)
            {
                int data = Convert.ToInt32(value);
                if (data >= 0)
                {
                    throw new ArgumentNullException(name, string.Format("Method '{0}' Parameter '{1}' Value '{2}' is not greater or equal zero.", methodName, name, value));
                }
            }

            throw new ArgumentNullException(name, string.Format("Method '{0}' Parameter '{1}' Value '{2}' is null.", methodName, name, value));
        }
    }

    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    public class LesserThanZero : Attribute
    {
        public static void Process(string methodName, string name, ref object value)
        {
            if (value != null)
            {
                int data = Convert.ToInt32(value);
                if (data >= 0)
                {
                    throw new ArgumentNullException(name, string.Format("Method '{0}' Parameter '{1}' Value '{2}' is not lesser than zero.", methodName, name, value));
                }
            }

            throw new ArgumentNullException(name, string.Format("Method '{0}' Parameter '{1}' Value '{2}' is null.", methodName, name, value));
        }
    }

    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    public class LesserOrEqualZero : Attribute
    {
        public static void Process(string methodName, string name, ref object value)
        {
            if (value != null)
            {
                int data = Convert.ToInt32(value);
                if (data <= 0)
                {
                    throw new ArgumentNullException(name, string.Format("Method '{0}' Parameter '{1}' Value '{2}' is not lesser or equal zero.", methodName, name, value));
                }
            }

            throw new ArgumentNullException(name, string.Format("Method '{0}' Parameter '{1}' Value '{2}' is null.", methodName, name, value));
        }
    }

    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    public class EmailValidator : Attribute
    {
        public static void Process(string methodName, string name, ref object value)
        {
            if (value != null)
            {
                string data = value.ToString();
                if (System.Text.RegularExpressions.Regex.IsMatch(data, @"^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$"))
                {
                    return;
                }
            }

            throw new ArgumentNullException(name, string.Format("Method '{0}' Parameter '{1}' Value '{2}' is not a valid E-Mail address.", methodName, name, value));
        }
    }

    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    public class Base64Validator : Attribute
    {
        public static void Process(string methodName, string name, ref object value)
        {
            if (value != null)
            {
                string data = value.ToString();
                if (IsBase64String(data))
                {
                    return;
                }                
            }

            throw new ArgumentNullException(name, string.Format("Method '{0}' Parameter '{1}' Value '{2}' is not a valid base64 string.", methodName, name, value));
        }

        private static Char[] Base64Chars = new[] { 'A', 'B', 'C', 'D', 'E', 'F', 'G', 'H', 'I', 'J', 'K', 'L', 'M', 'N', 'O', 'P', 'Q', 'R', 'S', 'T', 'U', 'V', 'W', 'X', 'Y', 'Z', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '+', '/' };

        public static bool IsBase64String(string value)
        {
            if (value == null || value.Length == 0 || value.Length % 4 != 0 || value.Contains(' ') || value.Contains('\t') || value.Contains('\r') || value.Contains('\n'))
            {
                return false;
            }

            var index = value.Length - 1;

            if (value[index] == '=')
            {
                index--;
            }

            if (value[index] == '=')
            {
                index--;
            }

            for (var i = 0; i <= index; i++)
            {
                if (!Base64Chars.Contains(value[i]))
                {
                    return false;
                }
            }

            return true;
        }
    }


}
