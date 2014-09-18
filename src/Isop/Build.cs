﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using Isop.Configuration;
using Isop.Controller;
using Isop.Help;
using Isop.Infrastructure;
using Isop.Lex;
using Isop.Parse;
using TypeConverterFunc = System.Func<System.Type, string, System.Globalization.CultureInfo, object>;
namespace Isop
{
    /// <summary>
    /// represents a configuration build
    /// </summary>
    public class Build : ConfigureUsingInstance,ICollection<Type>, IDisposable
    {
        private readonly IList<ArgumentWithOptions> _argumentRecognizers;
        private readonly IList<KeyValuePair<Type,Func<ControllerRecognizer>>> _controllerRecognizers;
        private HelpForControllers _helpForControllers;
        private HelpForArgumentWithOptions _helpForArgumentWithOptions;
        private HelpController _helpController;
        private readonly TypeContainer _container = new TypeContainer();
        public override Func<Type, object> Factory
        {
            get
            {
                return _container.Factory;
            }
            set
            {
                _container.Factory = value;
            }
        }

        public override TypeConverterFunc TypeConverter
        {
            get;
            set;
        }

        public override bool RecognizeHelp
        {
            get { return RecognizesHelp; }
            set
            {
                if (value)
                {
                    ShouldRecognizeHelp();
                }
                else if (RecognizesHelp)
                {
                    throw new NotImplementedException("! Deregister help controller etc");
                }
            }
        }

        public override IList<ArgumentWithOptions> ArgumentRecognizers
        {
            get { return _argumentRecognizers; }
        }

        readonly HelpXmlDocumentation _HelpXmlDocumentation = new HelpXmlDocumentation();

        public IEnumerator<Type> GetEnumerator()
        {
            return _controllerRecognizers.Select(cmr => cmr.Key).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(Type item)
        {
            Recognize(item);
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public bool Contains(Type item)
        {
            return _controllerRecognizers.Any(kv => kv.Key == item);
        }

        public void CopyTo(Type[] array, int arrayIndex)
        {
            throw new NotImplementedException();
        }

        public bool Remove(Type item)
        {
            throw new NotImplementedException();
        }

        public int Count
        {
            get { return _controllerRecognizers.Count; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public override ICollection<Type> Recognizes
        {
            get { return this; }
        }

        public override HelpXmlDocumentation HelpXmlDocumentation
        {
            get { return _HelpXmlDocumentation; }
        }

        public Build()
        {
            _controllerRecognizers = new List<KeyValuePair<Type,Func<ControllerRecognizer>>>();
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
            CultureInfo = cultureInfo; return this;
        }

        public Build SetTypeConverter(TypeConverterFunc typeconverter)
        {
            TypeConverter = typeconverter; return this;
        }

        public Build SetFactory(Func<Type, Object> factory)
        {
            _container.Factory = factory;
            return this;
        }

        public ParsedArguments Parse(IEnumerable<string> arg)
        {
            return Parse(arg.ToList());
        }

        public ParsedArguments Parse(List<string> arg)
        {
            var argumentParser = new ArgumentParser(_argumentRecognizers, _allowInferParameter);
            var lexed = ArgumentLexer.Lex(arg).ToList();
            var parsedArguments = argumentParser.Parse(lexed, arg);
            if (_controllerRecognizers.Any())
            {
                var recognizers = _controllerRecognizers.Select(cr => cr.Value());
                var controllerRecognizer = recognizers.FirstOrDefault(recognizer => recognizer.Recognize(arg));
                if (null != controllerRecognizer)
                {
                    return controllerRecognizer.ParseArgumentsAndMerge(parsedArguments,
                                                                       parsedMethod => parsedMethod.Factory = _container.CreateInstance);
                }
            }
            parsedArguments.AssertFailOnUnMatched();
            return parsedArguments;
        }

        public Build Recognize(Type arg, CultureInfo cultureInfo = null, TypeConverterFunc typeConverter = null, bool ignoreGlobalUnMatchedParameters = false)
        {
            _controllerRecognizers.Add(new KeyValuePair<Type, Func<ControllerRecognizer>>(arg, ()=> new ControllerRecognizer(arg,
                cultureInfo ?? CultureInfo,
                typeConverter ?? TypeConverter,
                ignoreGlobalUnMatchedParameters,
                _allowInferParameter)));
            return this;
        }
        public Build Recognize(Object arg, CultureInfo cultureInfo = null, TypeConverterFunc typeConverter = null, bool ignoreGlobalUnMatchedParameters = false)
        {
            var type = arg.GetType();
            _controllerRecognizers.Add(new KeyValuePair<Type, Func<ControllerRecognizer>>(type, () => new ControllerRecognizer(type,
               cultureInfo ?? CultureInfo,
               typeConverter ?? TypeConverter,
               ignoreGlobalUnMatchedParameters,
               _allowInferParameter)));
            _container.Instances.Add(type, arg);
            return this;
        }

        public Build DisallowInferParameter()
        {
            _allowInferParameter = false;
            return this;
        }

        public String Help()
        {
            var cout = new StringWriter(CultureInfo);
            Parse(new[] { "Help" }).Invoke(cout);
            return cout.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        public Build HelpTextCommandsAre(Action<HelpTexts> action)
        {
            ShouldRecognizeHelp();
            var helpForControllers = _helpForControllers;
            action(helpForControllers);
            return this;
        }

        public Build HelpTextArgumentsAre(string theArgumentsAre)
        {
            ShouldRecognizeHelp();
            _helpForArgumentWithOptions.TheArgumentsAre = theArgumentsAre;
            return this;
        }

        public string HelpFor(string controller, string action = null)
        {
            var cout = new StringWriter(CultureInfo);
            Parse(new[] { "Help", controller, action }
                .Where(s => !string.IsNullOrEmpty(s))).Invoke(cout);
            return cout.ToString();
        }

        public Build ShouldRecognizeHelp()
        {
            if (_helpController == null)
            {
                _helpForControllers = new HelpForControllers(Recognizes, _container, 
                    new TurnParametersToArgumentWithOptions(CultureInfo, TypeConverter), 
                    HelpXmlDocumentation);
                _helpForArgumentWithOptions = new HelpForArgumentWithOptions(_argumentRecognizers);
                _helpController = new HelpController(_helpForArgumentWithOptions, _helpForControllers);
                Recognize(_helpController, ignoreGlobalUnMatchedParameters: true);
            }
            return this;
        }

        public bool RecognizesHelp
        {
            get { return _helpController != null; }
        }

        public IEnumerable<KeyValuePair<Type, Func<ControllerRecognizer>>> ControllerRecognizers
        {
            get { return _controllerRecognizers; }
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
            return Configuration(typeof(T), instance);
        }
        public Build Configuration(Type t, object instance)
        {
            Configure(t, instance);

            if (instance is IDisposable)
                disposables.Add((IDisposable)instance);

            return this;
        }

        public Build Configuration(Type type)
        {
            return Configuration(type, Activator.CreateInstance(type));
        }

        public Build ConfigurationFrom(string path)
        {
            var files = Directory.GetFiles(path)
                .Where(f =>
                {
                    var ext = Path.GetExtension(f);
                    return ext.EqualsIC(".dll") || ext.EqualsIC(".exe");
                })
                .Where(f => !Path.GetFileNameWithoutExtension(f).StartsWithIC("Isop"));
            foreach (var file in files)
            {
                var assembly = Assembly.LoadFile(file);
                var isopconfigurations = assembly.GetTypes()
                    .Where(type => type.Name.EqualsIC("isopconfiguration"));
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
}