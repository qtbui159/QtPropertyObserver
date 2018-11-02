using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Qt.Attributes
{
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class RaisePropertyChangedAttribute : Attribute
    {
        public RaisePropertyChangedAttribute()
        {
        }
    }
}
