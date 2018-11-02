using Harmony;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Qt
{
    static class HookHelper
    {
        private static HarmonyInstance m_HookHelper = HarmonyInstance.Create(Guid.NewGuid().ToString());

        public static void Patch(HookEntity hookEntity, MethodInfo hookProc)
        {
            if (hookEntity == null)
            {
                throw new ArgumentNullException(nameof(hookEntity));
            }
            if (hookProc == null)
            {
                throw new ArgumentNullException(nameof(hookProc));
            }

            foreach (MethodBase method in hookEntity.WillBeHookSetter)
            {
                m_HookHelper.Patch(method, null, new HarmonyMethod(hookProc), null);
            }
        }
    }
}
