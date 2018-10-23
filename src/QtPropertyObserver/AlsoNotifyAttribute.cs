using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Qt
{
    /// <summary>
    /// 同时通知其他的属性进行更新
    /// </summary>
    public sealed class AlsoNotifyAttribute : Attribute
    {
        public string[] PropertiesName { private set; get; }

        public AlsoNotifyAttribute(params string[] propertiesNames)
        {
            if (propertiesNames == null || propertiesNames.Length == 0)
            {
                //空数组就行了
                PropertiesName = new string[0];
            }
            else
            {
                PropertiesName = new string[propertiesNames.Length];
                propertiesNames.CopyTo(PropertiesName, 0);
            }
        }
    }
}
