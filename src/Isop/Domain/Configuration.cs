﻿using System;
using System.Globalization;
using System.Collections.Generic;

namespace Isop.Domain
{
    public class Configuration
    {
        public Configuration()
        {
            Recognizes = new List<Controller>();
            Properties = new List<Property>();
        }
        public CultureInfo CultureInfo { get; set; }
        public IList<Controller> Recognizes { get; private set; }
        public IList<Property> Properties { get; private set; }

        public Func<Type, object> Factory { get; set; }

        public Func<Type, string, CultureInfo, object> TypeConverter { get; set; }

        public bool RecognizesHelp { get; set; }
    }
}

