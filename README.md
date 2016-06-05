# ILRewriter
Rewrites .NET IL to support AOP features like custom attributes (using Mono.Cecil).

###Configuration

To enable IL rewrite, add a post build action for your project:

```
ILRewriter.exe "$(TargetPath)"
```

###Method Attributes
To intercept methods, you just have to define a custom attribute (or use the default ones from the library):

```csharp
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class MethodLogging : Attribute
    {
        private Stopwatch stopWatch;

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
```

The attribute just need to have 2 methods with the name ```PreMethod``` and ```PostMethod``` (the name is important - see signature above). 

Now you can add your custom attribute to any method in your assembly. (e.g. for enabling logging):

```csharp
        [MethodLogging]
        static void DoSomething(string text, int zahl)
        {
            Console.WriteLine("DoSomething() method body. Text: " + text + " Zahl: " + zahl);
        }
```

Now the output in the console will look like this:

```
02.06.2016 03:34:41 Enter method: 'DoSomething' Parameter: 'einText, 3'
DoSomething() method body. Text: einText Zahl: 3
02.06.2016 03:34:41 Leaving method: 'DoSomething' Parameter: 'einText, 3' Duration: '1 ms'
```

###Parameter Attributes
To process parameters, you just have to define a custom attribute (or use the default ones from the library):

```csharp
    [AttributeUsage(AttributeTargets.Parameter, Inherited = false)]
    public class NotNull : Attribute
    {
        public static void Process(string methodName, string name, object value)
        {
            if(value == null)
            {
                throw new ArgumentNullException(name, string.Format("Method '{0}' Parameter '{1}' is null.",methodName, name));
            }
        }
    }
```

Now you can add your custom attribute to any parameter in your assembly. (e.g. for null checking):

```csharp
        static void DoSomething([NotNull]string text, int zahl)
        {
            Console.WriteLine("DoSomething() method body. Text: " + text + " Zahl: " + zahl);
        }
```

If you now call ```DoSomething(null, 3)``` (first parameter is null), a ArgumentNullException will be thrown (according to the custom  attribute).

###Property Attributes
To process properties, you just have to define a custom attribute (or use the default ones from the library):

```csharp
    [AttributeUsage(AttributeTargets.Property, Inherited = false)]
    public class PropertyNotNull : Attribute
    {
        public static void Get(string propertyName, object value)
        {
            if(value == null)
            {
                throw new ArgumentNullException(propertyName, string.Format("Property '{0}' Method 'Get' is null.", propertyName));
            }
        }

        public static void Set(string propertyName, object value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(propertyName, string.Format("Property '{0}' Method 'Set' is null.", propertyName));
            }
        }
    }
```

Now you can add your custom attribute to any property in your assembly. (e.g. for null checking):

```csharp
        private string dataString = "data";
        [PropertyNotNull]
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
```

Now, on every ```Get``` or ```Set``` call, the value will be validated.
