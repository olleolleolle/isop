﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Isop.Controller;
using Isop.Help;
using Isop.Lex;
using Isop.Parse;
using TypeConverterFunc=System.Func<System.Type,string,System.Globalization.CultureInfo,object>;
namespace Isop
{
    /// <summary>
    /// represents a configuration build
    /// </summary>
    public class Build:IDisposable
    {
        private readonly IList<ArgumentWithOptions> _argumentRecognizers;
        private readonly IList<Func<ControllerRecognizer>> _controllerRecognizers;
        public CultureInfo Culture { get; private set; }
        public TypeConverterFunc TypeConverter { get; private set; }
        private HelpForControllers _helpForControllers;
        private HelpForArgumentWithOptions _helpForArgumentWithOptions;
        private HelpController _helpController;
        private readonly TypeContainer _container=new TypeContainer();
        private readonly HelpXmlDocumentation helpXmlDocumentation = new HelpXmlDocumentation();
        public Build()
        {
            _controllerRecognizers = new List<Func<ControllerRecognizer>>();
            _argumentRecognizers = new List<ArgumentWithOptions>();
        }

        public Build Parameter(ArgumentParameter argument, Action<string> action = null, bool required = false, string description = null)
        {
            _argumentRecognizers.Add(new ArgumentWithOptions(argument, action, required, description, typeof(string)));
            return this;
        }
        /// <summary>
        /// Sets the cultureinfo for the following calls.
        /// </summary>
        /// <param name="cultureInfo"></param>
        /// <returns></returns>
        public Build SetCulture(CultureInfo cultureInfo)
        {
            Culture = cultureInfo; return this;
        }

        public Build SetTypeConverter(TypeConverterFunc typeconverter)
        {
            TypeConverter = typeconverter; return this;
        }
		
		public Build SetFactory(Func<Type,Object> factory)
		{
		    _container.Factory=factory;
		    return this;
		}
		
        public ParsedArguments Parse(IEnumerable<string> arg)
        {
			var argumentParser = new ArgumentParser(_argumentRecognizers, _allowInferParameter);
            // TODO: Need to figure out where this goes. To Much logic for this layer.
            var lexer = new ArgumentLexer(arg);
            var parsedArguments = argumentParser.Parse(lexer, arg);
            if (_controllerRecognizers.Any())
            {
                var recognizers = _controllerRecognizers.Select(cr=>cr());
                var controllerRecognizer = recognizers.FirstOrDefault(recognizer => recognizer.Recognize(arg));
                if (null != controllerRecognizer)
                {
					var parsedMethod = controllerRecognizer.Parse(arg);
					parsedMethod.Factory = _container.CreateInstance;
                    // Inferred ordinal arguments should not be recognized twice
                    parsedArguments.RecognizedArguments = parsedArguments.RecognizedArguments
                        .Where(argopts=>!parsedMethod.RecognizedArguments.Any(pargopt=>pargopt.Index == argopts.Index && argopts.InferredOrdinal));
                    var merged = parsedArguments.Merge( parsedMethod);
                    if (!controllerRecognizer.IgnoreGlobalUnMatchedParameters)
                        FailOnUnMatched(merged);
                    return merged;
                }
            }
            FailOnUnMatched(parsedArguments);
            return parsedArguments;
        }

        private static void FailOnUnMatched(ParsedArguments parsedArguments)
        { // This does not belong here. This is just supposed to be a fluent layer.
            var unMatchedRequiredArguments = parsedArguments.UnMatchedRequiredArguments();

            if (unMatchedRequiredArguments.Any())
            {
                throw new MissingArgumentException("Missing arguments")
                          {
                              Arguments = unMatchedRequiredArguments
                                  .Select(
                                      unmatched =>
                                      new KeyValuePair<string, string>(unmatched.Argument.ToString(), unmatched.Argument.Help()))
                                  .ToList()
                          };
            }
        }

        public Build Recognize(Type arg, CultureInfo cultureInfo = null, TypeConverterFunc typeConverter = null, bool ignoreGlobalUnMatchedParameters=false)
        {
            _controllerRecognizers.Add(()=>new ControllerRecognizer(arg, 
                cultureInfo?? Culture, 
                typeConverter ?? TypeConverter,
                ignoreGlobalUnMatchedParameters,
                _allowInferParameter));
            return this;
        }
		public Build Recognize(Object arg, CultureInfo cultureInfo = null, TypeConverterFunc typeConverter = null, bool ignoreGlobalUnMatchedParameters=false)
        {
            _controllerRecognizers.Add(() => new ControllerRecognizer(arg.GetType(),
               cultureInfo?? Culture ,
               typeConverter ?? TypeConverter, 
               ignoreGlobalUnMatchedParameters, 
               _allowInferParameter));
            _container.Instances.Add(arg.GetType(),arg);
            return this;
        }

        public Build DisallowInferParameter()
        {
            _allowInferParameter = false;
            return this;
        }

        public String Help()
        {
            var cout = new StringWriter(Culture);
            Parse(new []{"Help"}).Invoke(cout);
			return cout.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="theCommandsAre">default: "The commands are:"</param>
        /// <param name="helpCommandForMoreInformation">default: "Se 'COMMANDNAME' help command for more information"</param>
        /// <param name="theSubCommandsFor">default: The sub commands for </param>
        /// <param name="helpSubCommandForMoreInformation">default: Se 'COMMANDNAME' help 'command' 'subcommand' for more information</param>
        /// <returns></returns>
        public Build HelpTextCommandsAre(string theCommandsAre,
            string helpCommandForMoreInformation,
            string theSubCommandsFor,
            string helpSubCommandForMoreInformation)
        {
            RecognizeHelp();
            var helpForControllers = _helpForControllers;
            helpForControllers.TheCommandsAre = theCommandsAre;
            helpForControllers.HelpCommandForMoreInformation = helpCommandForMoreInformation;
            helpForControllers.TheSubCommandsFor = theSubCommandsFor;
            helpForControllers.HelpSubCommandForMoreInformation = helpSubCommandForMoreInformation;
            return this;
        }
        
        public Build HelpTextArgumentsAre(string theArgumentsAre)
        {
            RecognizeHelp();
            _helpForArgumentWithOptions.TheArgumentsAre = theArgumentsAre;
            return this;
        }

        public string HelpFor(string controller, string action=null)
        {
            var cout = new StringWriter(Culture);
            Parse( new[] { "Help", controller, action }
                .Where(s=>!string.IsNullOrEmpty(s))).Invoke(cout);
            return cout.ToString();
        }

        public Build RecognizeHelp()
        {
            if (_helpController==null)
            {
                _helpForControllers = new HelpForControllers(_controllerRecognizers, _container, helpXmlDocumentation);
                _helpForArgumentWithOptions = new HelpForArgumentWithOptions(_argumentRecognizers);
                _helpController = new HelpController(_helpForArgumentWithOptions, _helpForControllers);
                Recognize(_helpController, ignoreGlobalUnMatchedParameters:true);
            }
            return this;
        }

        public bool RecognizesHelp
        {
            get { return _helpController!=null; }
        }

        public IEnumerable<ControllerRecognizer> ControllerRecognizers
        {
            get { return _controllerRecognizers.Select(cr=>cr()); }
        }

        public IEnumerable<ArgumentWithOptions> GlobalParameters
        {
            get { return _argumentRecognizers; }
        }

        public Func<Type, object> GetFactory()
        {
            return _container.CreateInstance;
        }
        private List<IDisposable> disposables = new List<IDisposable>();
        private bool _allowInferParameter = true;

        public void Dispose()
        {
            foreach (var item in disposables) 
            {
                item.Dispose();
            }
            disposables.Clear();
        }
        
        public Build Configuration<T>(T instance)
        {
            return Configuration(typeof(T),instance);
        }
        public Build Configuration(Type t,object instance)
        {
            var methods= t.GetMethods(BindingFlags.Instance | BindingFlags.Public);
            var recognizer = new MethodInfoFinder();

            var culture = recognizer.MatchGet(methods, 
                name:"Culture",
                returnType: typeof(CultureInfo),
                parameters: new Type[0]);
            if (null!=culture)
                Culture = (CultureInfo)culture.Invoke(instance,new object[0]);
            
            var recognizesMethod = recognizer.MatchGet(methods, 
                name:"Recognizes",
                returnType: typeof(IEnumerable<Type>),
                parameters: new Type[0]);
            if (null!=recognizesMethod)
            {
                var recognizes=(IEnumerable<Type>)recognizesMethod.Invoke(instance, new object[0]);
                foreach (var recognized in recognizes)
                {
                    Recognize(recognized);
                }
            }
            var objectFactory = recognizer.Match(methods,
                name: "ObjectFactory",
                returnType: typeof(object),
                parameters: new[] { typeof(Type) });
            if (null!=objectFactory)
                SetFactory((Func<Type, object>)Delegate.CreateDelegate(typeof(Func<Type, object>), instance, objectFactory));

            var configurationSetters = recognizer.FindSet(methods);
            foreach (var methodInfo in configurationSetters)
            {
                var action = (Action<String>)Delegate.CreateDelegate(typeof(Action<String>), 
                    instance, methodInfo.MethodInfo);
                var description = helpXmlDocumentation.GetDescriptionForMethod(methodInfo.MethodInfo);
                this.Parameter(RemoveSetFromBeginningOfString(methodInfo.Name),
                    action:action,
                    description:description,
                    required: methodInfo.Required);//humz? required?
            }
            var _recongizeHelp = recognizer.MatchGet(methods,
                name:"RecognizeHelp",
                returnType: typeof(bool),
                parameters: new Type[0]);
                
            if (null!=_recongizeHelp && (bool)_recongizeHelp.Invoke(instance,new object[0]))
            {
                RecognizeHelp() ;
            }
   
            var _typeconv = recognizer.MatchGet(methods,
                name:"TypeConverter",
                returnType: typeof(TypeConverterFunc),
                parameters: new Type[0]);
            if (null!=_typeconv)
            {
                SetTypeConverter((TypeConverterFunc)_typeconv.Invoke(instance,new object[0]));
            }
            
            if (instance is IDisposable) 
                disposables.Add((IDisposable)instance);
            
            return this;            
        }
        private string RemoveSetFromBeginningOfString(string arg)
        {
            if (arg.StartsWith("set_",StringComparison.OrdinalIgnoreCase))
                return arg.Substring(4);
            if (arg.StartsWith("set",StringComparison.OrdinalIgnoreCase))
                return arg.Substring(3);
            return arg;
        }
        public Build Configuration(Type type)
        {
            return Configuration(type,Activator.CreateInstance(type));
        }

        public Build ConfigurationFrom (string path)
        {
            var files = Directory.GetFiles(path)
                .Where(f=>{
                    var ext = Path.GetExtension(f);
                    return ext.Equals(".dll") || ext.Equals(".exe");
                })
                .Where(f=>!Path.GetFileNameWithoutExtension(f).StartsWith("Isop"));
            foreach (var file in files) 
            {
                var assembly= Assembly.LoadFile(file);
                var isopconfigurations = assembly.GetTypes()
                    .Where(type=>type.Name.Equals("isopconfiguration",StringComparison.OrdinalIgnoreCase));
                foreach (var config in isopconfigurations) 
                {
                   Configuration(config);
                }
                if (!isopconfigurations.Any())
                {
                    new IsopAutoConfiguration(assembly)
                        .AddToConfiguration(this);
                }
            }
            return this;
        }

        public HelpController HelpController()
        {
            if (_helpController != null) return _helpController;
            return null;
        }
    }
    public class IsopAutoConfiguration
    {
        private Assembly _assembly;
        public IsopAutoConfiguration (Assembly assembly)
        {
          _assembly = assembly;
        }
        public IEnumerable<Type> Recognizes()
        {
            return _assembly.GetTypes().Where(t=>
                t.IsPublic
                && t.Name.EndsWith("controller",StringComparison.OrdinalIgnoreCase) 
                && t.GetConstructors().Any(ctor=>ctor.GetParameters().Length==0)
                );
        }
        public void AddToConfiguration(Build build)
        {
            foreach (var item in Recognizes()) {
                build.Recognize(item);
            }
        }
    }
}