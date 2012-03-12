using System;
using System.Text;

namespace Isop
{
    public class HelpController
    {
        readonly HelpForArgumentWithOptions _helpForArgumentWithOptions;
        readonly HelpForControllers _helpForClassAndMethod;

        public HelpController(
            HelpForArgumentWithOptions helpForArgumentWithOptions, 
            HelpForControllers helpForClassAndMethod)
        {
            _helpForArgumentWithOptions = helpForArgumentWithOptions;
            _helpForClassAndMethod = helpForClassAndMethod;
        }

        public string Index()
        {
            var sb = new StringBuilder();
            if (_helpForArgumentWithOptions.CanHelp())
            {
              sb.AppendLine(_helpForArgumentWithOptions.Help());
            }
            if (_helpForClassAndMethod.CanHelp())
            {
                sb.AppendLine(_helpForClassAndMethod.Help());
            }
            return sb.ToString().Trim(' ','\t','\r','\n');
        }
        
        public string Index(string command)
        {
         if (String.IsNullOrEmpty(command))
             return Index();
            var sb = new StringBuilder();
            if (_helpForArgumentWithOptions.CanHelp(command))
            {
              sb.AppendLine(_helpForArgumentWithOptions.Help(command));
            }
            if (_helpForClassAndMethod.CanHelp(command))
            {
                sb.AppendLine(_helpForClassAndMethod.Help(command));
            }
            return sb.ToString().Trim(' ','\t','\r','\n');
        }
    }
}

