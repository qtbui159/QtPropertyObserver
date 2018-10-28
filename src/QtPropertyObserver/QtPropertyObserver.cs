using Harmony;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Input;

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
        /// Key为T的Type，Value为这个Type的PropertyChanged event
        /// </summary>
        private static Dictionary<Type, FieldInfo> m_TypeMapPropertyChangedField = new Dictionary<Type, FieldInfo>();
        /// <summary>
        /// Key为ICommand的Type,Value为这个ICommand的CanExecuteChanged event
        /// </summary>
        private static Dictionary<Type, FieldInfo> m_TypeMapCanExecuteChangedField = new Dictionary<Type, FieldInfo>();
        /// <summary>
        /// Key为T的Type，Value为这个Type下需要Notify的属性集合
        /// 属性集合的Key为属性名,Value为需要额外通知的属性名
        /// </summary>
        private static Dictionary<Type, Dictionary<string, List<string>>> m_TypeMapProperties = new Dictionary<Type, Dictionary<string, List<string>>>();
        /// <summary>
        /// Key为T的Type，Value为这个Type下需要调用CanExecuteChanged的属性集合
        /// 属性集合的Key为属性名,Value为需要额外更新CanExecute的Command的PropertyInfo
        /// </summary>
        private static Dictionary<Type, Dictionary<string, List<PropertyInfo>>> m_TypeMapCommandsProperty = new Dictionary<Type, Dictionary<string, List<PropertyInfo>>>();
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

                //得到该属性的所有Custom Attributes

                Dictionary<Type, List<object>> customAttributes = GetCustomAttributes(x, new List<Type>() { typeof(IgnoreNotifyAttribute), typeof(AlsoNotifyAttribute), typeof(RaiseCanExecuteChangedAttribute) });

                if (customAttributes.ContainsKey(typeof(IgnoreNotifyAttribute)))
                {
                    //忽略
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
                if (customAttributes.ContainsKey(typeof(AlsoNotifyAttribute)))
                {
                    HashSet<string> propertiesNames = new HashSet<string>();
                    propertiesNames.Add(x.Name);

                    List<object> attrAlsoNotify = customAttributes[typeof(AlsoNotifyAttribute)];
                    if (attrAlsoNotify != null && attrAlsoNotify.Count != 0)
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

                    //加入要通知的额外属性

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
                else
                {
                    //没有额外的，加入自己就行
                    if (!m_TypeMapProperties.ContainsKey(m_ObserveType))
                    {
                        m_TypeMapProperties.Add(m_ObserveType, new Dictionary<string, List<string>>());
                    }
                    if (!m_TypeMapProperties[m_ObserveType].ContainsKey(x.Name))
                    {
                        m_TypeMapProperties[m_ObserveType].Add(x.Name, new List<string>());
                    }
                    m_TypeMapProperties[m_ObserveType][x.Name].Add(x.Name);
                }
                
                //加入要额外更新的Command
                if (customAttributes.ContainsKey(typeof(RaiseCanExecuteChangedAttribute)))
                {
                    HashSet<PropertyInfo> commands = new HashSet<PropertyInfo>();
                    foreach (RaiseCanExecuteChangedAttribute raise in customAttributes[typeof(RaiseCanExecuteChangedAttribute)])
                    {
                        foreach (string cmd in raise.Commands)
                        {
                            PropertyInfo propertyInfo = m_ObserveType.GetProperty(cmd, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                            if (propertyInfo == null)
                            {
                                continue;
                            }
                            commands.Add(propertyInfo);
                        }
                    }
                    

                    if (commands.Count == 0)
                    {
                        //没有的话不管
                        return;
                    }

                    if (!m_TypeMapCommandsProperty.ContainsKey(m_ObserveType))
                    {
                        m_TypeMapCommandsProperty.Add(m_ObserveType, new Dictionary<string, List<PropertyInfo>>());
                    }

                    if (!m_TypeMapCommandsProperty[m_ObserveType].ContainsKey(x.Name))
                    {
                        m_TypeMapCommandsProperty[m_ObserveType].Add(x.Name, new List<PropertyInfo>());
                    }
                    m_TypeMapCommandsProperty[m_ObserveType][x.Name].AddRange(commands);
                }
            }
        }

        /// <summary>
        /// 根据属性取需要的自定义Attribute，如果没有找到Attribute，返回的Dictionary里面没有该Key
        /// </summary>
        /// <param name="propertyInfo"></param>
        /// <param name="typeList"></param>
        /// <returns></returns>
        private static Dictionary<Type,List<object>> GetCustomAttributes(PropertyInfo propertyInfo, List<Type> typeList)
        {
            Dictionary<Type, List<object>> result = new Dictionary<Type, List<object>>();

            if (propertyInfo == null)
            {
                return result;
            }
            if (typeList == null || typeList.Count == 0)
            {
                return result;
            }

            object[] allCustomerAttributes = propertyInfo.GetCustomAttributes(false);

            foreach (object attr in allCustomerAttributes)
            {
                foreach (Type type in typeList)
                {
                    if (attr.GetType() != type)
                    {
                        continue;
                    }

                    if (result.ContainsKey(type))
                    {
                        result[type].Add(attr);
                    }
                    else
                    {
                        result.Add(type, new List<object>() { attr });
                    }
                }
            }

            return result;
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
                FieldInfo tmpFieldInfo = GetFieldInfo(typeof(T), nameof(INotifyPropertyChanged.PropertyChanged), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

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

            //需要额外更新的ICommand
            HashSet<ICommand> commands = new HashSet<ICommand>();
            if (m_TypeMapCommandsProperty.ContainsKey(typeof(T)) && m_TypeMapCommandsProperty[typeof(T)].ContainsKey(propertyName))
            {
                foreach (PropertyInfo prop in m_TypeMapCommandsProperty[typeof(T)][propertyName])
                {
                    ICommand cmd = prop.GetValue(__instance, null) as ICommand;
                    if (cmd == null)
                    {
                        continue;
                    }

                    commands.Add(cmd);
                }
            }

            foreach (var cmd in commands)
            {
                if (!m_TypeMapCanExecuteChangedField.ContainsKey(cmd.GetType()))
                {
                    FieldInfo tmpFieldInfo = GetFieldInfo(cmd.GetType(), nameof(ICommand.CanExecuteChanged), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (tmpFieldInfo == null)
                    {
                        continue;
                    }
                    m_TypeMapCanExecuteChangedField.Add(cmd.GetType(), tmpFieldInfo);
                }

                FieldInfo canExecuteEventField = m_TypeMapCanExecuteChangedField[cmd.GetType()];
                MulticastDelegate canExecuteDelegate = canExecuteEventField.GetValue(cmd) as MulticastDelegate;
                if (canExecuteDelegate == null)
                {
                    //没绑定任何事件
                    continue;
                }

                foreach (var x in canExecuteDelegate.GetInvocationList())
                {
                    x.Method.Invoke(x.Target, new object[] { cmd, EventArgs.Empty });
                }
            }
        }

        /// <summary>
        /// 遍历本类及父类寻找field,失败返回null
        /// </summary>
        /// <param name="instanceType"></param>
        /// <param name="fieldName"></param>
        /// <param name="bindingFlags"></param>
        /// <returns></returns>
        private static FieldInfo GetFieldInfo(Type instanceType, string fieldName, BindingFlags bindingFlags)
        {
            Type objType = typeof(object);
            Type tmpType = instanceType;

            FieldInfo fieldInfo = null;

            while (tmpType != objType)
            {
                if ((fieldInfo = tmpType.GetField(fieldName, bindingFlags)) == null)
                {
                    tmpType = tmpType.BaseType;
                }
                else
                {
                    break;
                }
            }

            return fieldInfo;
        }
    }
}
