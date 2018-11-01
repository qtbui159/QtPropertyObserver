using Qt.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;

namespace Qt
{
    public static partial class QtPropertyObserver
    {
        private static Dictionary<Type, HookEntity> m_ObserveType = new Dictionary<Type, HookEntity>();
        
        /// <summary>
        /// Hook函数的信息
        /// </summary>
        private static MethodInfo m_HookProc = null;

        static QtPropertyObserver()
        {
            m_HookProc = typeof(QtPropertyObserver).GetMethod(nameof(QtPropertyObserver.PropertyChanged), BindingFlags.NonPublic | BindingFlags.Static);
        }

        private static void Complete(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            
            if (m_ObserveType.ContainsKey(type))
            {
                //已经存在了
                return;
            }

            //取得所有Setter和Attribute，接下来组装HookEntity
            HookEntity hookEntity = new HookEntity(type);

            Dictionary<MethodBase, List<object>> setMethodMapAttributes = GetSetterWithAttributesFromType(type, AttributeFilter);
            foreach (var kvp in setMethodMapAttributes)
            {
                hookEntity.SavePropertyChain(kvp.Key, kvp.Value);
            }
            m_ObserveType.Add(type, hookEntity);

            //组装完毕，进行Patch

            HookHelper.Patch(m_ObserveType[type], m_HookProc);
        }

        /// <summary>
        /// 取一个属性的setter
        /// </summary>
        /// <param name="prop"></param>
        /// <returns></returns>
        private static MethodBase GetSetMethod(PropertyInfo prop)
        {
            if (prop == null)
            {
                return null;
            }

            MethodBase methodBase = prop.GetSetMethod(false);
            if (methodBase != null)
            {
                return methodBase;
            }
            return prop.GetSetMethod(true);
        }

        private static List<object> AttributeFilter(IEnumerable<object> attributes)
        {
            List<object> attributeList = new List<object>();

            if (attributes == null || attributes.Count() == 0)
            {
                return attributeList;
            }

            List<Type> attributeFilterList = new List<Type>()
            {
                typeof(RaisePropertyChangedAttribute),
                typeof(RaiseOtherPropertyChangedAttribute),
                typeof(RaiseCanExecuteChangedAttribute),
            };

            IEnumerator<object> etor = attributes.GetEnumerator();
            while (etor.MoveNext())
            {
                object obj = etor.Current;
                if (obj == null)
                {
                    continue;
                }
                Type type = obj.GetType();
                if (!attributeFilterList.Contains(type))
                {
                    continue;
                }
                attributeList.Add(obj);
            }

            return attributeList;
        }

        /// <summary>
        /// 取一个类型的全部setter(不包含父类)
        /// </summary>
        /// <param name="type"></param>
        /// <param name="attributesFilter">通过过滤Attribute，只保留需要的Setter</param>
        /// <returns></returns>
        private static Dictionary<MethodBase, List<object>> GetSetterWithAttributesFromType(Type type, Func<IEnumerable<object>, List<object>> attributesFilter = null)
        {
            Dictionary<MethodBase, List<object>> setterMapAttribute = new Dictionary<MethodBase, List<object>>();

            if (type == null)
            {
                return setterMapAttribute;
            }

            foreach (PropertyInfo prop in type.GetProperties(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
            {
                MethodBase methodBase = GetSetMethod(prop);
                if (methodBase == null)
                {
                    continue;
                }
                if (attributesFilter == null)
                {
                    setterMapAttribute.Add(methodBase, prop.GetCustomAttributes(false).ToList());
                }
                else
                {
                    List<object> leftAttributes = attributesFilter(prop.GetCustomAttributes(false));
                    if (leftAttributes.Count == 0)
                    {
                        continue;
                    }
                    setterMapAttribute.Add(methodBase, leftAttributes);
                }
            }

            return setterMapAttribute;
        }

        /// <summary>
        /// 执行完Setter后的方法
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="__originalMethod"></param>
        private static void PropertyChanged(object __instance, MethodBase __originalMethod)
        {
            INotifyPropertyChanged notifyPropertyChangedInstance = __instance as INotifyPropertyChanged;
            if (notifyPropertyChangedInstance == null)
            {
                return;
            }

            if (__originalMethod == null)
            {
                return;
            }

            Type type = __instance.GetType();
            if (!m_ObserveType.ContainsKey(type))
            {
                return;
            }

            HookEntity hookEntity = m_ObserveType[type];

            hookEntity.OnPropertyChanged(__instance, __originalMethod);
            hookEntity.OnCanExecuteChanged(__instance, __originalMethod);
        }
    }

    public static partial class QtPropertyObserver
    {
        public static void Complete<T>() where T : INotifyPropertyChanged
        {
            Complete(typeof(T));
        }
    }
}
