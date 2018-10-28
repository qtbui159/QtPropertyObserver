using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;

namespace Qt
{
    /// <summary>
    /// 同时通知其他的属性进行更新
    /// </summary>
    public sealed class AlsoNotifyAttribute : Attribute
    {
        public string[] PropertiesName { private set; get; } = new string[0];

        public AlsoNotifyAttribute(params string[] propertiesNames)
        {
            if (propertiesNames == null || propertiesNames.Length == 0)
            {
                return;
            }

            List<string> propertyList = propertiesNames.Where(x => !string.IsNullOrWhiteSpace(x)).ToList();

            PropertiesName = new string[propertyList.Count];
            propertyList.CopyTo(PropertiesName, 0);
        }
    }
}
