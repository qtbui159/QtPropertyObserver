using Harmony;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Qt
{
    public class QtPropertyObserver<T> where T : INotifyPropertyChanged
    {
        private Type m_Type = typeof(T);
        private HarmonyInstance m_Instance = HarmonyInstance.Create(Guid.NewGuid().ToString());
        private MethodInfo m_Method = null;

        private static Dictionary<Type, FieldInfo> m_TypeMapPropertyChangedField = new Dictionary<Type, FieldInfo>();
        private static Dictionary<string, string> m_SetMethodNameMapPropertyName = new Dictionary<string, string>();

        public QtPropertyObserver()
        {
            m_Method = this.GetType().GetMethod(nameof(NotifyPropertyChanged), BindingFlags.Static | BindingFlags.NonPublic);

            PropertyInfo[] observerProperty = m_Type.GetProperties().Where(x => x.DeclaringType == m_Type).ToArray();

            foreach (var x in observerProperty)
            {
                //寻找public,private,protected 3种的setter

                MethodInfo tmpMethodInfo = x.GetSetMethod(true);
                if (tmpMethodInfo == null)
                {
                    tmpMethodInfo = x.GetSetMethod(false);
                    if (tmpMethodInfo == null)
                    {
                        continue;
                    }
                }
                m_Instance.Patch(tmpMethodInfo, null, new HarmonyMethod(m_Method));
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
            
            foreach (var x in eventDelegate.GetInvocationList())
            {
                x.Method.Invoke(x.Target, new object[] {__instance,new PropertyChangedEventArgs(propertyName) });
            }
        }
    }
}
