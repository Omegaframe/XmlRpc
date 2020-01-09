using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace XmlRpc.Client
{
    static class SystemHelper
    {
        /// <summary>
        /// System's Type.GetMethods() does not return methods that a derived interface inherits from its base interfaces. 
        /// This method does.
        /// </summary>
        public static IEnumerable<MethodInfo> GetMethods(Type type)
        {
            var baseMethods = type.GetMethods();
            var interfaceMethods = type.GetInterfaces().SelectMany(t => t.GetMethods());

            return baseMethods.Concat(interfaceMethods).Distinct();
        }
    }
}
