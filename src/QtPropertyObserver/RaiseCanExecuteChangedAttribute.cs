using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Qt
{
    public sealed class RaiseCanExecuteChangedAttribute : Attribute
    {
        public string[] Commands { private set; get; } = new string[0];

        public RaiseCanExecuteChangedAttribute(params string[] commands)
        {
            if (commands == null || commands.Length == 0)
            {
                return;
            }

            List<string> commandList = commands.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();
            Commands = new string[commandList.Count];
            commandList.CopyTo(Commands, 0);
        }
    }
}
