# Isop [![Build Status](https://travis-ci.org/wallymathieu/isop.png?branch=isop)](https://travis-ci.org/wallymathieu/isop) [![Build status](https://ci.appveyor.com/api/projects/status/r4fbt9onjg3yfojv/branch/isop?svg=true)](https://ci.appveyor.com/project/wallymathieu/isop/branch/isop)
## The name

Isop is the swedish name for hyssop. Like any spice it is intended to give flavor to the development of command line apps in .net. 

## Goal

The goal is to be able to write code like:
<pre><code>someprogram.exe My Action --argument value</code></pre>
Or if you prefer:
<pre><code>someprogram.exe My Action /argument value</code></pre>
Isop will also figure out what you mean if you write with an equals sign between argument and value:
<pre><code>someprogram.exe My Action --argument=value</code></pre>
Or if you want to write it shorter you can skip the argument name:
<pre><code>someprogram.exe My Action value</code></pre>

So that the class with the name My or MyController and the method with the name Action gets invoked.

This library is intended to be like chocolate pudding mix. Not something that will replace your dinner, but rather something easy to make for dessert. A way of helping you build for instance the essential administrative apps. It's not a replacement for baking cake (building a full blown administrative interface in html, silverlight, wpf ... ). 

## License

MIT License

## Nuget packages

<a href="http://nuget.org/packages/Isop/">Isop</a>

## Example

### Having your own Main

You're hooking it up by writing something like:
<pre><code>static void Main(string[] args)
{
  new Build()
       .Recognize(typeof(CustomerController))
       .Parse(args)
       .Invoke(Console.Out);
}</code></pre>

Where your controller looks something like this:
<pre><code>
public class MyController
{
    private readonly CustomerRepository _repository;
    public MyController()
    {
        _repository = new CustomerRepository();
    }
    public IEnumerable&lt;string&gt; Add(string name)
    { 
        yield return "Starting to insert customer";
        _repository.Insert( new Customer{ Name = name } );
        yield return "Customer inserted";  
    }
}
</code></pre>
When invoked it will output two lines to the command prompt, the yielded lines above.

### Using a configuration class for isop

<pre><code>
class IsopConfiguration:IDisposable
{
...
    public IEnumerable<Type> Recognizes()
    {
        return new[] { typeof(CustomerController) };
    }
    public object ObjectFactory(Type type)
    {
        return _myIOC.Resolve(type);
    }
    public CultureInfo Culture
    {
        get{ return CultureInfo.GetCultureInfo("sv-SE"); }
    }
    public bool RecognizeHelp{get{return true;}}
    public void Dispose()
    {
        _myIOC.Dispose();
    }
}
class Program
{
    static void Main(string[] args)
    {
        var configuration = new IsopConfiguration();
        new Build()
          .Configuration(configuration)
          .Parse(args)
          .Invoke(Console.Out);
    }
}</code></pre>

This is equivalent to the following fluent configuration of isop:

<pre><code>
class Program
{
    static void Main(string[] args)
    {
        new Build()
          .SetCulture(CultureInfo.GetCultureInfo("sv-SE"))
          .Recognize(typeof(CustomerController))
          .SetFactory(_myIOC.Resolve);
          .ShouldRecognizeHelp()
          .Parse(args)
          .Invoke(Console.Out);
    }
}</code></pre>

### Handling errors and unrecognized parameters

<pre><code>
class Program
{
    static void Main(string[] args)
    {
        var parserBuilder = new Build()
                  .SetCulture(CultureInfo.GetCultureInfo("sv-SE"))
                  .Recognize(typeof(CustomerController))
                  .SetFactory(_myIOC.Resolve);
                  .ShouldRecognizeHelp();
        try
        {
            var parsedMethod = parserBuilder.Parse(args);
            if (parsedMethod.UnRecognizedArguments.Any())//Warning:
            {
                var unRecognizedArgumentsMessage = string.Format(
@"Unrecognized arguments: 
{0}
Did you mean any of these arguments?
{1}", String.Join(",", parsedMethod.UnRecognizedArguments.Select(unrec => unrec.Value).ToArray()),
  String.Join(",", parsedMethod.ArgumentWithOptions.Select(rec => rec.Argument.ToString()).ToArray()));
                Console.WriteLine(unRecognizedArgumentsMessage);
            }else
            {
                parsedMethod.Invoke(Console.Out);
            }
        }
        catch (TypeConversionFailedException ex)
        {
            
             Console.WriteLine(String.Format("Could not convert argument {0} with value {1} to type {2}", 
                ex.Argument, ex.Value, ex.TargetType));
             if (null!=ex.InnerException)
            {
                Console.WriteLine("Inner exception: ");
                Console.WriteLine(ex.InnerException.Message);
            }
        }
        catch (MissingArgumentException ex)
        {
            Console.WriteLine(String.Format("Missing argument(s): {0}",String.Join(", ",ex.Arguments.Select(a=>String.Format("{0}: {1}",a.Key,a.Value)).ToArray())));
            
            Console.WriteLine(parserBuilder.Help());
        }

    }
}</code></pre>

Why all this code? Mostly it's because I want the programmer to be able to have as much freedom as possible to handle errors and show error messages as he/she sees fit.

### Using Isop.cli.exe

You're hooking it up by writing something like:
<pre><code> class IsopConfiguration
{
    public IEnumerable<Type> Recognizes()
    {
        return new[] { typeof(CustomerController) };
    }
    public string Global {
        get;
        set;
    }
    [Required]
    public string GlobalRequired
    {
        get;
        set;
    }
    public object ObjectFactory(Type type)
    {
        return _myIOC.Resolve(type);
    }
    public CultureInfo Culture
    {
        get{ return CultureInfo.GetCultureInfo("es-ES"); }
    }
    public bool RecognizeHelp{get{return true;}}
    
    public Func<Type, string, CultureInfo, object> GetTypeconverter()
    {
        return TypeConverter;
    }
}
</code></pre>

This configuration will have a Global variable (that is, a parameter that can be set for any action). It will have a required global parameter (you must set that parameter in order to run any action). Note that you can insert your own type converter (how the strings will be converted to different values in the controller action parameters).

Why enter culture code? Isop tries as much as possible to adhere to the user specified culturecode. This have some implications for people using sv-SE or any other culture where the standard date formatting does not fit.

Since Isop does not know your IOC it provides a hook (ObjectFactory) in order to be able to resolve the controllers.

The same configuration class can be consumed by the fluent api: 

<pre><code>
static void Main(string[] args)
{
    var configuration = new IsopConfiguration();
    new Build()
      .Configuration(configuration)
      .Parse(args)
      .Invoke(Console.Out);
}
</code></pre>

You can invoke your program by (where you have Isop.Cli.exe in the same folder as your dll or exe containing the above class)
<pre><code>Isop.Cli.exe Customer Add --name value</code></pre>

Look at the <a href="/wallymathieu/isop/blob/master/Example/Program.cs">Example Cli project</a> for the most recent example of how it is used. 

## Runners
[Nuget feed for isop project on appveyor](https://ci.appveyor.com/nuget/isop-22q278hpwhwk)

## Alternative

The alternative for this kind of library is <a href="http://lostechies.com/chadmyers/2011/06/06/cool-stuff-in-fubucore-no-6-command-line/">the framework found in fubucore</a>.

If you want to have something simpler for simple command line applications then I would recommend using <a href="https://github.com/wallymathieu/ndesk-options-mirror">ndesc options</a>.
