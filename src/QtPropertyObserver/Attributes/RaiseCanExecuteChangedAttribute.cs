using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Qt.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class RaiseCanExecuteChangedAttribute : Attribute
    {
        public HashSet<string> CommandNames { private set; get; }

        public RaiseCanExecuteChangedAttribute(params string[] commandNames)
        {
            CommandNames = new HashSet<string>();

            if (commandNames == null)
            {
                return;
            }

            foreach (string name in commandNames.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                CommandNames.Add(name);
            }
        }
    }
}
