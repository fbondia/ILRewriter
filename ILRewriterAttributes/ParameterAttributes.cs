﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ILRewriterAttributes
{
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    public class NotNull : Attribute
    {
        public static void Process(string methodName, string name, object value)
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
        public static void Process(string methodName, string name, object value)
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
    public class Email : Attribute
    {
        public static void Process(string methodName, string name, object value)
        {
            if (value != null)
            {
                string data = value.ToString();
                if(System.Text.RegularExpressions.Regex.IsMatch(data, @"^\w+([-+.']\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$"))
                {
                    return;
                }
            }

            throw new ArgumentNullException(name, string.Format("Method '{0}' Parameter '{1}' Value '{2}' is not a valid E-Mail address.", methodName, name, value));
        }
    }
}
