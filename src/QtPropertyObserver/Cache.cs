using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Qt
{
    /// <summary>
    /// 缓存，专门存储Setter 和Property 的对应名
    /// </summary>
    static class Cache
    {
        /// <summary>
        /// 保存Setter Name和Property Name的键值对关系
        /// </summary>
        private static Dictionary<string, string> m_SetterMapProperty = new Dictionary<string, string>();

        /// <summary>
        /// 将Setter Name转换为Property Name
        /// </summary>
        /// <param name="setterName"></param>
        /// <returns></returns>
        public static string ConvertSetterToPropertyName(string setterName)
        {
            const string setterPrefix = "set_";

            if (string.IsNullOrWhiteSpace(setterName))
            {
                return "";
            }

            if (m_SetterMapProperty.ContainsKey(setterName))
            {
                return m_SetterMapProperty[setterName];
            }

            if (!setterName.StartsWith(setterPrefix))
            {
                return "";
            }

            string propertyName = setterName.Substring(setterPrefix.Length, setterName.Length - setterPrefix.Length);
            m_SetterMapProperty.Add(setterName, propertyName);
            return propertyName;
        }

        /// <summary>
        /// 将Setter Name转换为Property Name
        /// </summary>
        /// <param name="setterName"></param>
        /// <returns></returns>
        public static string ConvertSetterToPropertyName(MethodBase method)
        {
            if (method == null)
            {
                return "";
            }
            return ConvertSetterToPropertyName(method.Name);
        }
    }
}
