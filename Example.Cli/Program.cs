using System;
using System.Collections.Generic;

namespace Example.Cli
{
    /// <summary>
    /// This is a sample usage of Isop when you want to invoke the program using Isop.Cli.exe:
    /// </summary>
    class IsopConfiguration
    {
        public IEnumerable<Type> Recognizes()
        {
            return new[] { typeof(MyController), typeof(CustomerController) };
        }
        public string Global { get; set; }
        public bool RecognizeHelp{get{return true;}}
    }
    
    public class MyController
    {
        public string Action(string value)
        {
            return "invoking action on mycontroller with value : " + value;
        }
        public string Fail()
        {
            throw new Exception("Failure!");
        }
        public string ActionWithGlobalParameter(string global)
        {
            return "invoking action with global parameter on mycontroller with value " + global;
        }
        public class Argument
	{
            public string MyProperty { get; set; }

	}
        public string ActionWithObjectArgument(Argument arg) 
        {
            return "Invoking ActionWithObjectArgument " + arg.MyProperty;
        }
    }
    public class CustomerController
    {
        public string Add(string name)
        {
            return "invoking action Add on customercontroller with name : " + name;
        }
    }
}
