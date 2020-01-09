using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using XmlRpc.Client.Attributes;
using XmlRpc.Client.Exceptions;

namespace XmlRpc.Client
{
    public static class XmlRpcProxyGen
    {
        static readonly Hashtable _types = new Hashtable();

        public static T Create<T>() where T : IXmlRpcProxy
        {
            return (T)Create(typeof(T));
        }

        public static object Create(Type serviceType)
        {
            if (!serviceType.IsInterface)
                throw new XmlRpcServiceIsNoInterfaceException($"Requested Type {serviceType.Name} is no interface but has to be one.");

            if (!typeof(IXmlRpcProxy).IsAssignableFrom(serviceType))
                throw new XmlRpcServiceInterfaceNotImplementedException($"Requested Type {serviceType.Name} does not implement required interface {nameof(IXmlRpcProxy)}");

            var proxyType = (Type)_types[serviceType] ?? BuildServiceType(serviceType);
            return Activator.CreateInstance(proxyType);
        }

        static Type BuildServiceType(Type serviceType)
        {
            const string assemblyNamePrefix = "XmlRpc_";

            var guid = Guid.NewGuid().ToString();
            var randomName = assemblyNamePrefix + guid;
            var assemblyBuilder = BuildAssembly(serviceType, randomName, randomName, AssemblyBuilderAccess.Run);

            return assemblyBuilder.GetType(randomName);
        }

        static AssemblyBuilder BuildAssembly(Type serviceType, string assemblyName, string typeName, AssemblyBuilderAccess access)
        {
            var serviceUri = GetXmlRpcUri(serviceType);
            var methods = GetXmlRpcMethods(serviceType);

            ArrayList beginMethods = GetXmlRpcBeginMethods(serviceType);
            ArrayList endMethods = GetXmlRpcEndMethods(serviceType);
            AssemblyName assName = new AssemblyName();
            assName.Name = assemblyName;
            if (access == AssemblyBuilderAccess.Run)
                assName.Version = serviceType.Assembly.GetName().Version;
            AssemblyBuilder assBldr = AssemblyBuilder.DefineDynamicAssembly(assName, access);
            ModuleBuilder modBldr = assBldr.DefineDynamicModule(assName.Name);
            TypeBuilder typeBldr = modBldr.DefineType(
              typeName,
              TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.Public,
              typeof(XmlRpcClientProtocol),
              new Type[] { serviceType });
            BuildConstructor(typeBldr, typeof(XmlRpcClientProtocol), serviceUri);
            BuildMethods(typeBldr, methods);
            BuildBeginMethods(typeBldr, beginMethods);
            BuildEndMethods(typeBldr, endMethods);
            typeBldr.CreateTypeInfo();

            return assBldr;
        }

        static void BuildMethods(TypeBuilder tb, ArrayList methods)
        {
            foreach (MethodData mthdData in methods)
            {
                MethodInfo mi = mthdData.Mi;
                Type[] argTypes = new Type[mi.GetParameters().Length];
                string[] paramNames = new string[mi.GetParameters().Length];
                for (int i = 0; i < mi.GetParameters().Length; i++)
                {
                    argTypes[i] = mi.GetParameters()[i].ParameterType;
                    paramNames[i] = mi.GetParameters()[i].Name;
                }
                XmlRpcMethodAttribute mattr = (XmlRpcMethodAttribute)
                  Attribute.GetCustomAttribute(mi, typeof(XmlRpcMethodAttribute));
                BuildMethod(tb, mi.Name, mthdData.XmlRpcName, paramNames, argTypes,
                  mthdData.IsParamsMethod, mi.ReturnType, mattr.StructParams);
            }
        }

        static void BuildMethod(
          TypeBuilder tb,
          string methodName,
          string rpcMethodName,
          string[] paramNames,
          Type[] argTypes,
          bool paramsMethod,
          Type returnType,
          bool structParams)
        {
            MethodBuilder mthdBldr = tb.DefineMethod(
              methodName,
              MethodAttributes.Public | MethodAttributes.Virtual,
              returnType, argTypes);
            // add attribute to method
            Type[] oneString = new Type[1] { typeof(string) };
            Type methodAttr = typeof(XmlRpcMethodAttribute);
            ConstructorInfo ci = methodAttr.GetConstructor(oneString);
            PropertyInfo[] pis
              = new PropertyInfo[] { methodAttr.GetProperty("StructParams") };
            object[] structParam = new object[] { structParams };
            CustomAttributeBuilder cab =
              new CustomAttributeBuilder(ci, new object[] { rpcMethodName },
                pis, structParam);
            mthdBldr.SetCustomAttribute(cab);
            for (int i = 0; i < paramNames.Length; i++)
            {
                ParameterBuilder paramBldr = mthdBldr.DefineParameter(i + 1,
                  ParameterAttributes.In, paramNames[i]);
                // possibly add ParamArrayAttribute to final parameter
                if (i == paramNames.Length - 1 && paramsMethod)
                {
                    ConstructorInfo ctorInfo = typeof(ParamArrayAttribute).GetConstructor(
                      new Type[0]);
                    CustomAttributeBuilder attrBldr =
                      new CustomAttributeBuilder(ctorInfo, new object[0]);
                    paramBldr.SetCustomAttribute(attrBldr);
                }
            }
            // generate IL
            ILGenerator ilgen = mthdBldr.GetILGenerator();
            // if non-void return, declared locals for processing return value
            LocalBuilder retVal = null;
            LocalBuilder tempRetVal = null;
            if (typeof(void) != returnType)
            {
                tempRetVal = ilgen.DeclareLocal(typeof(object));
                retVal = ilgen.DeclareLocal(returnType);
            }
            // declare variable to store method args and emit code to populate ut
            LocalBuilder argValues = ilgen.DeclareLocal(typeof(object[]));
            ilgen.Emit(OpCodes.Ldc_I4, argTypes.Length);
            ilgen.Emit(OpCodes.Newarr, typeof(object));
            ilgen.Emit(OpCodes.Stloc, argValues);
            for (int argLoad = 0; argLoad < argTypes.Length; argLoad++)
            {
                ilgen.Emit(OpCodes.Ldloc, argValues);
                ilgen.Emit(OpCodes.Ldc_I4, argLoad);
                ilgen.Emit(OpCodes.Ldarg, argLoad + 1);
                if (argTypes[argLoad].IsValueType)
                {
                    ilgen.Emit(OpCodes.Box, argTypes[argLoad]);
                }
                ilgen.Emit(OpCodes.Stelem_Ref);
            }
            // call Invoke on base class
            Type[] invokeTypes = new Type[] { typeof(MethodInfo), typeof(object[]) };
            MethodInfo invokeMethod
              = typeof(XmlRpcClientProtocol).GetMethod("Invoke", invokeTypes);
            ilgen.Emit(OpCodes.Ldarg_0);
            ilgen.Emit(OpCodes.Call, typeof(MethodBase).GetMethod("GetCurrentMethod"));
            ilgen.Emit(OpCodes.Castclass, typeof(System.Reflection.MethodInfo));
            ilgen.Emit(OpCodes.Ldloc, argValues);
            ilgen.Emit(OpCodes.Call, invokeMethod);
            //  if non-void return prepare return value, otherwise pop to discard 
            if (typeof(void) != returnType)
            {
                // if return value is null, don't cast it to required type
                Label retIsNull = ilgen.DefineLabel();
                ilgen.Emit(OpCodes.Stloc, tempRetVal);
                ilgen.Emit(OpCodes.Ldloc, tempRetVal);
                ilgen.Emit(OpCodes.Brfalse, retIsNull);
                ilgen.Emit(OpCodes.Ldloc, tempRetVal);
                if (true == returnType.IsValueType)
                {
                    ilgen.Emit(OpCodes.Unbox, returnType);
                    ilgen.Emit(OpCodes.Ldobj, returnType);
                }
                else
                {
                    ilgen.Emit(OpCodes.Castclass, returnType);
                }
                ilgen.Emit(OpCodes.Stloc, retVal);
                ilgen.MarkLabel(retIsNull);
                ilgen.Emit(OpCodes.Ldloc, retVal);
            }
            else
            {
                ilgen.Emit(OpCodes.Pop);
            }
            ilgen.Emit(OpCodes.Ret);
        }

        static void BuildBeginMethods(TypeBuilder tb, ArrayList methods)
        {
            foreach (MethodData mthdData in methods)
            {
                MethodInfo mi = mthdData.Mi;
                // assume method has already been validated for required signature   
                int paramCount = mi.GetParameters().Length;
                // argCount counts of params before optional AsyncCallback param
                int argCount = paramCount;
                Type[] argTypes = new Type[paramCount];
                for (int i = 0; i < mi.GetParameters().Length; i++)
                {
                    argTypes[i] = mi.GetParameters()[i].ParameterType;
                    if (argTypes[i] == typeof(System.AsyncCallback))
                        argCount = i;
                }
                MethodBuilder mthdBldr = tb.DefineMethod(
                  mi.Name,
                  MethodAttributes.Public | MethodAttributes.Virtual,
                  mi.ReturnType,
                  argTypes);
                // add attribute to method
                Type[] oneString = new Type[1] { typeof(string) };
                Type methodAttr = typeof(XmlRpcBeginAttribute);
                ConstructorInfo ci = methodAttr.GetConstructor(oneString);
                CustomAttributeBuilder cab =
                  new CustomAttributeBuilder(ci, new object[] { mthdData.XmlRpcName });
                mthdBldr.SetCustomAttribute(cab);
                // start generating IL
                ILGenerator ilgen = mthdBldr.GetILGenerator();
                // declare variable to store method args and emit code to populate it
                LocalBuilder argValues = ilgen.DeclareLocal(typeof(object[]));
                ilgen.Emit(OpCodes.Ldc_I4, argCount);
                ilgen.Emit(OpCodes.Newarr, typeof(object));
                ilgen.Emit(OpCodes.Stloc, argValues);
                for (int argLoad = 0; argLoad < argCount; argLoad++)
                {
                    ilgen.Emit(OpCodes.Ldloc, argValues);
                    ilgen.Emit(OpCodes.Ldc_I4, argLoad);
                    ilgen.Emit(OpCodes.Ldarg, argLoad + 1);
                    ParameterInfo pi = mi.GetParameters()[argLoad];
                    string paramTypeName = pi.ParameterType.AssemblyQualifiedName;
                    paramTypeName = paramTypeName.Replace("&", "");
                    Type paramType = Type.GetType(paramTypeName);
                    if (paramType.IsValueType)
                    {
                        ilgen.Emit(OpCodes.Box, paramType);
                    }
                    ilgen.Emit(OpCodes.Stelem_Ref);
                }
                // emit code to store AsyncCallback parameter, defaulting to null 
                // if not in method signature
                LocalBuilder acbValue = ilgen.DeclareLocal(typeof(System.AsyncCallback));
                if (argCount < paramCount)
                {
                    ilgen.Emit(OpCodes.Ldarg, argCount + 1);
                    ilgen.Emit(OpCodes.Stloc, acbValue);
                }
                // emit code to store async state parameter, defaulting to null 
                // if not in method signature
                LocalBuilder objValue = ilgen.DeclareLocal(typeof(object));
                if (argCount < (paramCount - 1))
                {
                    ilgen.Emit(OpCodes.Ldarg, argCount + 2);
                    ilgen.Emit(OpCodes.Stloc, objValue);
                }
                // emit code to call BeginInvoke on base class
                Type[] invokeTypes = new Type[]
              {
        typeof(MethodInfo),
        typeof(object[]),
        typeof(object),
        typeof(AsyncCallback),
        typeof(object)
              };
                MethodInfo invokeMethod
                  = typeof(XmlRpcClientProtocol).GetMethod("BeginInvoke", invokeTypes);
                ilgen.Emit(OpCodes.Ldarg_0);
                ilgen.Emit(OpCodes.Call, typeof(MethodBase).GetMethod("GetCurrentMethod"));
                ilgen.Emit(OpCodes.Castclass, typeof(System.Reflection.MethodInfo));
                ilgen.Emit(OpCodes.Ldloc, argValues);
                ilgen.Emit(OpCodes.Ldarg_0);
                ilgen.Emit(OpCodes.Ldloc, acbValue);
                ilgen.Emit(OpCodes.Ldloc, objValue);
                ilgen.Emit(OpCodes.Call, invokeMethod);
                // BeginInvoke will leave IAsyncResult on stack - leave it there
                // for return value from method being built
                ilgen.Emit(OpCodes.Ret);
            }
        }

        static void BuildEndMethods(TypeBuilder tb, ArrayList methods)
        {
            LocalBuilder retVal = null;
            LocalBuilder tempRetVal = null;
            foreach (MethodData mthdData in methods)
            {
                MethodInfo mi = mthdData.Mi;
                Type[] argTypes = new Type[] { typeof(System.IAsyncResult) };
                MethodBuilder mthdBldr = tb.DefineMethod(mi.Name,
                  MethodAttributes.Public | MethodAttributes.Virtual,
                  mi.ReturnType, argTypes);
                // start generating IL
                ILGenerator ilgen = mthdBldr.GetILGenerator();
                // if non-void return, declared locals for processing return value
                if (typeof(void) != mi.ReturnType)
                {
                    tempRetVal = ilgen.DeclareLocal(typeof(object));
                    retVal = ilgen.DeclareLocal(mi.ReturnType);
                }
                // call EndInvoke on base class
                Type[] invokeTypes
                  = new Type[] { typeof(System.IAsyncResult), typeof(System.Type) };
                MethodInfo invokeMethod
                  = typeof(XmlRpcClientProtocol).GetMethod("EndInvoke", invokeTypes);
                Type[] GetTypeTypes
                  = new Type[] { typeof(System.String) };
                MethodInfo GetTypeMethod
                  = typeof(System.Type).GetMethod("GetType", GetTypeTypes);
                ilgen.Emit(OpCodes.Ldarg_0);  // "this"
                ilgen.Emit(OpCodes.Ldarg_1);  // IAsyncResult parameter
                ilgen.Emit(OpCodes.Ldstr, mi.ReturnType.AssemblyQualifiedName);
                ilgen.Emit(OpCodes.Call, GetTypeMethod);
                ilgen.Emit(OpCodes.Call, invokeMethod);
                //  if non-void return prepare return value otherwise pop to discard 
                if (typeof(void) != mi.ReturnType)
                {
                    // if return value is null, don't cast it to required type
                    Label retIsNull = ilgen.DefineLabel();
                    ilgen.Emit(OpCodes.Stloc, tempRetVal);
                    ilgen.Emit(OpCodes.Ldloc, tempRetVal);
                    ilgen.Emit(OpCodes.Brfalse, retIsNull);
                    ilgen.Emit(OpCodes.Ldloc, tempRetVal);
                    if (true == mi.ReturnType.IsValueType)
                    {
                        ilgen.Emit(OpCodes.Unbox, mi.ReturnType);
                        ilgen.Emit(OpCodes.Ldobj, mi.ReturnType);
                    }
                    else
                    {
                        ilgen.Emit(OpCodes.Castclass, mi.ReturnType);
                    }
                    ilgen.Emit(OpCodes.Stloc, retVal);
                    ilgen.MarkLabel(retIsNull);
                    ilgen.Emit(OpCodes.Ldloc, retVal);
                }
                else
                {
                    // void method so throw away result from EndInvoke
                    ilgen.Emit(OpCodes.Pop);
                }
                ilgen.Emit(OpCodes.Ret);
            }
        }

        static void BuildConstructor(
          TypeBuilder typeBldr,
          Type baseType,
          string urlStr)
        {
            ConstructorBuilder ctorBldr = typeBldr.DefineConstructor(
              MethodAttributes.Public | MethodAttributes.SpecialName |
              MethodAttributes.RTSpecialName | MethodAttributes.HideBySig,
              CallingConventions.Standard,
              Type.EmptyTypes);
            if (urlStr != null && urlStr.Length > 0)
            {
                Type urlAttr = typeof(XmlRpcUrlAttribute);
                Type[] oneString = new Type[1] { typeof(string) };
                ConstructorInfo ci = urlAttr.GetConstructor(oneString);
                CustomAttributeBuilder cab =
                  new CustomAttributeBuilder(ci, new object[] { urlStr });
                typeBldr.SetCustomAttribute(cab);
            }
            ILGenerator ilgen = ctorBldr.GetILGenerator();
            //  Call the base constructor.
            ilgen.Emit(OpCodes.Ldarg_0);
            ConstructorInfo ctorInfo = baseType.GetConstructor(System.Type.EmptyTypes);
            ilgen.Emit(OpCodes.Call, ctorInfo);
            ilgen.Emit(OpCodes.Ret);
        }

        static string GetXmlRpcUri(Type itf)
        {
            var attr = Attribute.GetCustomAttribute(itf, typeof(XmlRpcUrlAttribute));
            if (attr == null)
                return null;

            var xruAttr = attr as XmlRpcUrlAttribute;
            return xruAttr.Uri;
        }

        static ArrayList GetXmlRpcMethods(Type serviceType)
        {
            var xmlRpcMethods = new ArrayList();
            foreach (var methodInfo in SystemHelper.GetMethods(serviceType))
            {
                var xmlRpcName = GetXmlRpcMethodName(methodInfo);
                if (string.IsNullOrWhiteSpace(xmlRpcName))
                    continue;

                var parameterInfos = methodInfo.GetParameters();
                var hasParamsParameter = Attribute.IsDefined(parameterInfos[^1], typeof(ParamArrayAttribute));
                var methodData = new MethodData(methodInfo, xmlRpcName, hasParamsParameter);

                xmlRpcMethods.Add(methodData);
            }

            return xmlRpcMethods;
        }

        static string GetXmlRpcMethodName(MethodInfo methodInfo)
        {
            var attribute = Attribute.GetCustomAttribute(methodInfo, typeof(XmlRpcMethodAttribute));
            if (attribute is XmlRpcMethodAttribute xmlAttribute)
                return string.IsNullOrWhiteSpace(xmlAttribute.Method) ? methodInfo.Name : xmlAttribute.Method;

            return null;
        }

        static ArrayList GetXmlRpcBeginMethods(Type serviceType)
        {
            const string beginStart = "Begin";

            var beginMethods = new ArrayList();
            foreach (var methodInfo in serviceType.GetMethods())
            {
                var attribute = Attribute.GetCustomAttribute(methodInfo, typeof(XmlRpcBeginAttribute));
                if (!(attribute is XmlRpcBeginAttribute rpcBeginAttribute))
                    continue;

                var rpcMethod = rpcBeginAttribute.Method;
                if (string.IsNullOrWhiteSpace(rpcMethod))
                {
                    if (!methodInfo.Name.StartsWith(beginStart) || methodInfo.Name.Equals(beginStart, StringComparison.OrdinalIgnoreCase))
                        throw new Exception($"method {methodInfo.Name} has invalid signature for begin method");

                    rpcMethod = methodInfo.Name.Substring(beginStart.Length);
                }

                var parameter = methodInfo.GetParameters();
                var paramCount = parameter.Length;
                var asyncParamPos = 0;

                for (asyncParamPos = 0; asyncParamPos < paramCount; asyncParamPos++)
                {
                    if (parameter[asyncParamPos].ParameterType == typeof(AsyncCallback))
                        break;
                }

                if (asyncParamPos < paramCount - 2)
                    throw new Exception($"method {methodInfo.Name} has invalid signature for begin method");

                if (asyncParamPos == paramCount - 2)
                {
                    var paramType = parameter[asyncParamPos + 1].ParameterType;
                    if (paramType != typeof(object))
                        throw new Exception($"method {methodInfo.Name} has invalid signature for begin method");
                }

                beginMethods.Add(new MethodData(methodInfo, rpcMethod, false, null));
            }

            return beginMethods;
        }

        static ArrayList GetXmlRpcEndMethods(Type serviceType)
        {
            var endMethods = new ArrayList();

            var attributeMethods = serviceType.GetMethods().Where(m => Attribute.GetCustomAttribute(m, typeof(XmlRpcEndAttribute)) is XmlRpcEndAttribute);
            foreach (var methodInfo in attributeMethods)
            {
                var parameterInfo = methodInfo.GetParameters();
                if (parameterInfo.Length != 1 || parameterInfo[0].ParameterType == typeof(IAsyncResult))
                    throw new Exception($"method {methodInfo.Name} has invalid signature for end method");

                endMethods.Add(new MethodData(methodInfo, string.Empty, false));
            }

            return endMethods;
        }
    }
}