using Qt.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Windows.Input;

namespace Qt
{
    class HookEntity
    {
        /// <summary>
        /// 被Hook的Type
        /// </summary>
        public Type HookType { private set; get; }
        /// <summary>
        /// 将要被Hook的Setter
        /// </summary>
        public HashSet<MethodBase> WillBeHookSetter { private set; get; }

        private FieldInfo m_PropertyChangedEvent = null;

        /// <summary>
        /// Key为Setter，Value为额外需要通知的Property
        /// </summary>
        private Dictionary<MethodBase, HashSet<string>> m_MethodMapOtherPropertyName = new Dictionary<MethodBase, HashSet<string>>();
        /// <summary>
        /// Key为Setter，Value为ICommand在HookType中的PropertyInfo
        /// </summary>
        private Dictionary<MethodBase, HashSet<ICommandInfo>> m_MethodMapCommandProperty = new Dictionary<MethodBase, HashSet<ICommandInfo>>();

        public HookEntity(Type host)
        {
            HookType = host;
            WillBeHookSetter = new HashSet<MethodBase>();
        }

        /// <summary>
        /// 将一个method的attributes保存在内部
        /// </summary>
        /// <param name="originMethod"></param>
        /// <param name="attributeList"></param>
        public void SavePropertyChain(MethodBase originMethod, List<object> attributeList)
        {
            if (originMethod == null)
            {
                throw new ArgumentNullException(nameof(originMethod));
            }

            if (attributeList == null)
            {
                throw new ArgumentNullException(nameof(attributeList));
            }

            /*
            if (attributeList.Count(x => x.GetType() == typeof(RaisePropertyChangedAttribute)) == 0)
            {
                //如果没有RaisePropertyChanged，就不处理这个setter了
                return;
            }*/

            WillBeHookSetter.Add(originMethod);

            foreach (object attr in attributeList)
            {
                if (attr == null)
                {
                    continue;
                }

                AttributeHandle(originMethod, attr);
            }
        }

        /// <summary>
        /// 处理一个method对应的attributes
        /// </summary>
        /// <param name="originMethod"></param>
        /// <param name="attr"></param>
        private void AttributeHandle(MethodBase originMethod, object attr)
        {
            if (attr.GetType() == typeof(RaiseOtherPropertyChangedAttribute))
            {
                RaiseOtherPropertyChangedAttribute raiseOtherPropertyChangedInstance = attr as RaiseOtherPropertyChangedAttribute;

                m_MethodMapOtherPropertyName.Add(originMethod, new HashSet<string>(raiseOtherPropertyChangedInstance.PropertyNames));
            }
            else if (attr.GetType() == typeof(RaiseCanExecuteChangedAttribute))
            {
                RaiseCanExecuteChangedAttribute raiseCanExecuteChangedInstance = attr as RaiseCanExecuteChangedAttribute;

                HashSet<ICommandInfo> commandInfo = new HashSet<ICommandInfo>();

                foreach (string commandName in raiseCanExecuteChangedInstance.CommandNames)
                {
                    PropertyInfo prop = HookType.GetProperty(commandName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.DeclaredOnly);
                    if (prop == null)
                    {
                        continue;
                    }
                    commandInfo.Add(new ICommandInfo() { PropertyInfo = prop });
                }

                m_MethodMapCommandProperty.Add(originMethod, commandInfo);
            }
        }

        public void OnPropertyChanged(object instance, MethodBase methodBase)
        {
            //因为PropertyChanged必须有this指针，所以instance和methodBase肯定都不是null

            if (instance == null || methodBase == null)
            {
                return;
            }

            if (m_PropertyChangedEvent == null)
            {
                m_PropertyChangedEvent = FindEvent(HookType, nameof(INotifyPropertyChanged.PropertyChanged));
                if (m_PropertyChangedEvent == null)
                {
                    return;
                }
            }

            HashSet<string> propertyNames = null;

            //统计一下要通知的其他属性，另外别忘了通知本体

            if (m_MethodMapOtherPropertyName.ContainsKey(methodBase))
            {
                propertyNames = new HashSet<string>(m_MethodMapOtherPropertyName[methodBase]);
                propertyNames.Add(Cache.ConvertSetterToPropertyName(methodBase));
            }
            else
            {
                propertyNames = new HashSet<string>() { Cache.ConvertSetterToPropertyName(methodBase) };
            }

            foreach (string propName in propertyNames)
            {
                InvokeEvent(m_PropertyChangedEvent, instance, new object[] { instance, new PropertyChangedEventArgs(propName) });
            }
        }

        public void OnCanExecuteChanged(object instance, MethodBase methodBase)
        {
            //这里的instance是指 实现了 INotifyPropertyChanged的实例，不是指ICommand的实例
            if (instance == null || methodBase == null)
            {
                return;
            }

            if (!m_MethodMapCommandProperty.ContainsKey(methodBase))
            {
                return;
            }

            foreach (ICommandInfo commandInfo in m_MethodMapCommandProperty[methodBase])
            {
                if (!commandInfo.FoundField)
                {
                    //需要寻找
                    commandInfo.FieldInfo = FindEvent(commandInfo.PropertyInfo.PropertyType, nameof(ICommand.CanExecuteChanged));
                    commandInfo.FoundField = true;
                }

                if (commandInfo.FieldInfo == null)
                {
                    continue;
                }

                object commandInstance = commandInfo.PropertyInfo.GetValue(instance, null);
                if (commandInstance == null)
                {
                    //为null的情况可能是该viewmodel正在创建中（构造函数对某项赋值），所以为null
                    continue;
                }
                InvokeEvent(commandInfo.FieldInfo, commandInstance, new object[] { commandInstance, EventArgs.Empty });
            }
        }

        public static void OnCanExecuteChanged(HookEntity hookEntity, MethodBase methodBase)
        {
            if (methodBase == null || hookEntity == null)
            {
                return;
            }

            if (!hookEntity.m_MethodMapCommandProperty.ContainsKey(methodBase))
            {
                return;
            }
            
            foreach (ICommandInfo commandInfo in hookEntity.m_MethodMapCommandProperty[methodBase])
            {
                if (!commandInfo.FoundField)
                {
                    //需要寻找
                    commandInfo.FieldInfo = FindEvent(commandInfo.PropertyInfo.PropertyType, nameof(ICommand.CanExecuteChanged));
                    commandInfo.FoundField = true;
                }

                if (commandInfo.FieldInfo == null)
                {
                    continue;
                }

                object commandInstance = commandInfo.PropertyInfo.GetValue(null, null);
                if (commandInstance == null)
                {
                    //为null的情况可能是该viewmodel正在创建中（构造函数对某项赋值），所以为null
                    continue;
                }
                InvokeEvent(commandInfo.FieldInfo, commandInstance, new object[] { commandInstance, EventArgs.Empty });
            }
        }

        /// <summary>
        /// 遍历所以寻找接口
        /// </summary>
        /// <param name="type"></param>
        /// <param name="eventName">接口名</param>
        /// <returns></returns>
        private static FieldInfo FindEvent(Type type, string eventName)
        {
            if (string.IsNullOrWhiteSpace(eventName))
            {
                return null;
            }
            if (type == null)
            {
                return null;
            }

            Type endType = typeof(object);
            Type nowType = type;

            FieldInfo fieldInfo = null;

            while (nowType != endType)
            {
                fieldInfo = nowType.GetField(eventName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (fieldInfo != null)
                {
                    break;
                }

                nowType = nowType.BaseType;
            }

            return fieldInfo;
        }

        private static void InvokeEvent(FieldInfo fieldInfo, object instance, params object[] param)
        {
            MulticastDelegate eventDelegate = fieldInfo.GetValue(instance) as MulticastDelegate;
            if (eventDelegate == null)
            {
                //没绑定任何事件
                return;
            }

            foreach (var x in eventDelegate.GetInvocationList())
            {
                x.Method.Invoke(x.Target, param);
            }
        }

    }

    class ICommandInfo
    {
        /// <summary>
        /// ICommand在Instance中的Property
        /// </summary>
        public PropertyInfo PropertyInfo { get; set; }
        /// <summary>
        /// ICommand的event
        /// </summary>
        public FieldInfo FieldInfo { get; set; }
        /// <summary>
        /// 如果FieldInfo为null的情况下，FoundField又为true，表示已经被搜索过了，只是没搜到
        /// </summary>
        public bool FoundField { get; set; } = false;

        /// <summary>
        /// 因为PropertyInfo相同的情况下，FieldInfo肯定也是一样的
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return PropertyInfo.GetHashCode();
        }
    }
}
