using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Qt.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class RaiseOtherPropertyChangedAttribute : Attribute
    {
        public HashSet<string> PropertyNames { private set; get; }

        public RaiseOtherPropertyChangedAttribute(params string[] propertyNames)
        {
            PropertyNames = new HashSet<string>();

            if (propertyNames == null)
            {
                return;
            }

            foreach (string name in propertyNames.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                PropertyNames.Add(name);
            }
        }
    }
}
