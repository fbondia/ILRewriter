# ILRewriter
Rewrites .NET IL to support AOP features like custom attributes (using Mono.Cecil).

To intercept methods, you just have to define a custom attribute (or use the default ones from the library):

```csharp
   [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class MethodLogging : Attribute
    {
        public static void PreMethod(string name)
        {
            Console.WriteLine(string.Format("{0} Enter method: '{1}'", DateTime.Now, name));
        }
        public static void PostMethod(string name)
        {
            Console.WriteLine(string.Format("{0} Leaving method: '{1}'", DateTime.Now, name));
        }
    }
```

The attribute just need to have 2 static methods with the name ```PreMethod``` and ```PostMethod``` (the name is important). 

To enable IL rewrite, add a post build action for your project:

```
ILRewriter.exe "$(TargetPath)"
```

Now you can add your custom attribute to any method in your assembly. (e.g. for enabling logging):

```csharp
        [MethodLogging]
        static void DoSomething()
        {
            Console.WriteLine("DoSomething() method body.");
        }
```

Now the output in the consoe will look like this:

```
02.06.2016 01:34:41 Enter method: 'DoSomething'
DoSomething() method body.
02.06.2016 01:34:41 Leaving method: 'DoSomething'
```
