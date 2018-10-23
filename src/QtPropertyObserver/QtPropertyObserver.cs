using Harmony;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Qt
{
    public class QtPropertyObserver<T> where T : INotifyPropertyChanged
    {
        private Type m_ObserveType = typeof(T);
        private HarmonyInstance m_Instance = HarmonyInstance.Create(Guid.NewGuid().ToString());

        /// <summary>
        /// 找到hook的函数
        /// </summary>
        private MethodInfo m_Method = null;

        /// <summary>
        /// Key为Type，Value为这个Type的PropertyChanged event
        /// </summary>
        private static Dictionary<Type, FieldInfo> m_TypeMapPropertyChangedField = new Dictionary<Type, FieldInfo>();
        /// <summary>
        /// Key为Type，Value为这个Type下需要Notify的属性集合
        /// 属性集合的Key为属性名,Value为需要额外通知的属性名
        /// </summary>
        private static Dictionary<Type, Dictionary<string, List<string>>> m_TypeMapProperties = new Dictionary<Type, Dictionary<string, List<string>>>();
        /// <summary>
        /// Key为SetMethod名字,Value为属性名
        /// </summary>
        private static Dictionary<string, string> m_SetMethodNameMapPropertyName = new Dictionary<string, string>();

        public QtPropertyObserver()
        {
            m_Method = this.GetType().GetMethod(nameof(NotifyPropertyChanged), BindingFlags.Static | BindingFlags.NonPublic);

            PropertyInfo[] observerProperty = m_ObserveType.GetProperties().Where(x => x.DeclaringType == m_ObserveType).ToArray();

            foreach (var x in observerProperty)
            {
                //寻找public,private,protected 3种的setter

                //忽略有IgnoreNotify的attribute

                object[] attrIgnoreNotify = x.GetCustomAttributes(typeof(IgnoreNotifyAttribute), false);
                if (attrIgnoreNotify != null && attrIgnoreNotify.Length != 0)
                {
                    //找到了,忽略这个属性
                    continue;
                }

                MethodInfo tmpMethodInfo = x.GetSetMethod(true);
                if (tmpMethodInfo == null)
                {
                    tmpMethodInfo = x.GetSetMethod(false);
                    if (tmpMethodInfo == null)
                    {
                        //没有Setter
                        continue;
                    }
                }
                m_Instance.Patch(tmpMethodInfo, null, new HarmonyMethod(m_Method));

                //确定该属性是否还要通知其他属性更新,没有就只加入自己
                HashSet<string> propertiesNames = new HashSet<string>();
                propertiesNames.Add(x.Name);

                object[] attrAlsoNotify = x.GetCustomAttributes(typeof(AlsoNotifyAttribute), false);
                if (attrAlsoNotify != null && attrAlsoNotify.Length != 0)
                {
                    //有的话还要加入待更新成员
                    foreach (AlsoNotifyAttribute attr in attrAlsoNotify)
                    {
                        foreach (string p in attr.PropertiesName)
                        {
                            if (string.IsNullOrWhiteSpace(p))
                            {
                                continue;
                            }
                            propertiesNames.Add(p);
                        }
                    }
                }

                if (!m_TypeMapProperties.ContainsKey(m_ObserveType))
                {
                    m_TypeMapProperties.Add(m_ObserveType, new Dictionary<string, List<string>>());
                }

                if (!m_TypeMapProperties[m_ObserveType].ContainsKey(x.Name))
                {
                    m_TypeMapProperties[m_ObserveType].Add(x.Name, new List<string>());
                }
                m_TypeMapProperties[m_ObserveType][x.Name].AddRange(propertiesNames);
            }
        }

        private static void NotifyPropertyChanged(T __instance, MethodBase __originalMethod)
        {
            if (__instance == null)
            {
                return;
            }

            if (!m_TypeMapPropertyChangedField.ContainsKey(typeof(T)))
            {
                //循环查找父类的event PropertyChanged
                Type objType = typeof(object);
                Type insType = __instance.GetType();
                FieldInfo tmpFieldInfo = null;

                while (insType != objType)
                {
                    tmpFieldInfo = insType.GetField(nameof(INotifyPropertyChanged.PropertyChanged), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (tmpFieldInfo == null)
                    {
                        insType = insType.BaseType;
                    }
                    else
                    {
                        break;
                    }
                }

                if (tmpFieldInfo == null)
                {
                    //没有找到
                    return;
                }

                m_TypeMapPropertyChangedField[typeof(T)] = tmpFieldInfo;
            }

            FieldInfo fieldInfo = m_TypeMapPropertyChangedField[typeof(T)];

            MulticastDelegate eventDelegate = fieldInfo.GetValue(__instance) as MulticastDelegate;
            if (eventDelegate == null)
            {
                //没绑定任何事件
                return;
            }

            if (!m_SetMethodNameMapPropertyName.ContainsKey(__originalMethod.Name))
            {
                string tmpPropertyName = __originalMethod.Name.Replace("set_", "");
                m_SetMethodNameMapPropertyName.Add(__originalMethod.Name, tmpPropertyName);
            }

            string propertyName = m_SetMethodNameMapPropertyName[__originalMethod.Name];

            //需要通知多个属性
            List<string> propertiesNames = new List<string>();
            if (m_TypeMapProperties[typeof(T)].ContainsKey(propertyName))
            {
                propertiesNames.AddRange(m_TypeMapProperties[typeof(T)][propertyName]);
            }
            else
            {
                propertiesNames.Add(propertyName);
            }

            foreach (var x in eventDelegate.GetInvocationList())
            {
                foreach (string p in propertiesNames)
                {
                    x.Method.Invoke(x.Target, new object[] { __instance, new PropertyChangedEventArgs(p) });
                }
            }
        }
    }
}
