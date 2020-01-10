using System;
using System.Collections;
using System.Collections.Generic;
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

        static AssemblyBuilder BuildAssembly(Type serviceType, string name, string typeName, AssemblyBuilderAccess access)
        {
            var serviceUri = GetXmlRpcUri(serviceType);
            var methods = GetXmlRpcMethods(serviceType);
            var beginMethods = GetXmlRpcBeginMethods(serviceType);
            var endMethods = GetXmlRpcEndMethods(serviceType);

            var assemblyName = new AssemblyName { Name = name };

            if (access == AssemblyBuilderAccess.Run)
                assemblyName.Version = serviceType.Assembly.GetName().Version;

            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, access);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);
            var typeBuilder = moduleBuilder.DefineType(typeName, TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.Public, typeof(XmlRpcClientProtocol), new Type[] { serviceType });

            BuildConstructor(typeBuilder, typeof(XmlRpcClientProtocol), serviceUri);
            BuildMethods(typeBuilder, methods);
            BuildBeginMethods(typeBuilder, beginMethods);
            BuildEndMethods(typeBuilder, endMethods);

            typeBuilder.CreateTypeInfo();

            return assemblyBuilder;
        }

        static void BuildMethods(TypeBuilder typeBuilder, MethodData[] methods)
        {
            foreach (var method in methods)
            {
                var methodInfo = method.MethodInfo;
                var parameters = methodInfo.GetParameters();

                var argTypes = new Type[parameters.Length];
                var paramNames = new string[parameters.Length];

                for (int i = 0; i < parameters.Length; i++)
                {
                    argTypes[i] = parameters[i].ParameterType;
                    paramNames[i] = parameters[i].Name;
                }

                var methodAttribute = (XmlRpcMethodAttribute)Attribute.GetCustomAttribute(methodInfo, typeof(XmlRpcMethodAttribute));
                BuildMethod(typeBuilder, methodInfo.Name, method.XmlRpcName, paramNames, argTypes, method.IsParamsMethod, methodInfo.ReturnType, methodAttribute.StructParams);
            }
        }

        static void BuildMethod(
          TypeBuilder typeBuilder,
          string methodName,
          string rpcMethodName,
          string[] paramNames,
          Type[] argTypes,
          bool paramsMethod,
          Type returnType,
          bool structParams)
        {
            var methodBuilder = typeBuilder.DefineMethod(methodName, MethodAttributes.Public | MethodAttributes.Virtual, returnType, argTypes);

            var stringType = new[] { typeof(string) };
            var methodAttribute = typeof(XmlRpcMethodAttribute);
            var construtorInfo = methodAttribute.GetConstructor(stringType);
            var protertyInfos = new[] { methodAttribute.GetProperty("StructParams") };
            var structParamArray = new object[] { structParams };
            var attributeBuilder = new CustomAttributeBuilder(construtorInfo, new object[] { rpcMethodName }, protertyInfos, structParamArray);
            methodBuilder.SetCustomAttribute(attributeBuilder);

            for (int i = 0; i < paramNames.Length; i++)
            {
                var paramBuilder = methodBuilder.DefineParameter(i + 1, ParameterAttributes.In, paramNames[i]);

                if (i == paramNames.Length - 1 && paramsMethod)
                {
                    var paramContructor = typeof(ParamArrayAttribute).GetConstructor(new Type[0]);
                    var paramAttributeBuilder = new CustomAttributeBuilder(paramContructor, new object[0]);
                    paramBuilder.SetCustomAttribute(paramAttributeBuilder);
                }
            }

            var ilGenerator = methodBuilder.GetILGenerator();
            LocalBuilder returnValue = null;
            LocalBuilder tempReturnValue = null;
            if (typeof(void) != returnType)
            {
                tempReturnValue = ilGenerator.DeclareLocal(typeof(object));
                returnValue = ilGenerator.DeclareLocal(returnType);
            }

            var argValues = ilGenerator.DeclareLocal(typeof(object[]));
            ilGenerator.Emit(OpCodes.Ldc_I4, argTypes.Length);
            ilGenerator.Emit(OpCodes.Newarr, typeof(object));
            ilGenerator.Emit(OpCodes.Stloc, argValues);
            for (var argLoad = 0; argLoad < argTypes.Length; argLoad++)
            {
                ilGenerator.Emit(OpCodes.Ldloc, argValues);
                ilGenerator.Emit(OpCodes.Ldc_I4, argLoad);
                ilGenerator.Emit(OpCodes.Ldarg, argLoad + 1);

                if (argTypes[argLoad].IsValueType)
                    ilGenerator.Emit(OpCodes.Box, argTypes[argLoad]);

                ilGenerator.Emit(OpCodes.Stelem_Ref);
            }

            var invokeTypes = new Type[] { typeof(MethodInfo), typeof(object[]) };
            var invokeMethod = typeof(XmlRpcClientProtocol).GetMethod("Invoke", invokeTypes);
            ilGenerator.Emit(OpCodes.Ldarg_0);
            ilGenerator.Emit(OpCodes.Call, typeof(MethodBase).GetMethod("GetCurrentMethod"));
            ilGenerator.Emit(OpCodes.Castclass, typeof(MethodInfo));
            ilGenerator.Emit(OpCodes.Ldloc, argValues);
            ilGenerator.Emit(OpCodes.Call, invokeMethod);

            if (typeof(void) != returnType)
            {
                var retIsNull = ilGenerator.DefineLabel();
                ilGenerator.Emit(OpCodes.Stloc, tempReturnValue);
                ilGenerator.Emit(OpCodes.Ldloc, tempReturnValue);
                ilGenerator.Emit(OpCodes.Brfalse, retIsNull);
                ilGenerator.Emit(OpCodes.Ldloc, tempReturnValue);
                if (true == returnType.IsValueType)
                {
                    ilGenerator.Emit(OpCodes.Unbox, returnType);
                    ilGenerator.Emit(OpCodes.Ldobj, returnType);
                }
                else
                {
                    ilGenerator.Emit(OpCodes.Castclass, returnType);
                }
                ilGenerator.Emit(OpCodes.Stloc, returnValue);
                ilGenerator.MarkLabel(retIsNull);
                ilGenerator.Emit(OpCodes.Ldloc, returnValue);
            }
            else
            {
                ilGenerator.Emit(OpCodes.Pop);
            }

            ilGenerator.Emit(OpCodes.Ret);
        }

        static void BuildBeginMethods(TypeBuilder typeBuilder, MethodData[] methods)
        {
            foreach (var methodData in methods)
            {
                var methodInfo = methodData.MethodInfo;
                var parameters = methodInfo.GetParameters();
                var paramCount = parameters.Length;
                var argCount = paramCount;
                var argTypes = new Type[paramCount];

                for (int i = 0; i < parameters.Length; i++)
                {
                    argTypes[i] = parameters[i].ParameterType;
                    if (argTypes[i] == typeof(AsyncCallback))
                        argCount = i;
                }

                var methodBuilder = typeBuilder.DefineMethod(methodInfo.Name, MethodAttributes.Public | MethodAttributes.Virtual, methodInfo.ReturnType, argTypes);

                var stringType = new[] { typeof(string) };
                var methodAttribute = typeof(XmlRpcBeginAttribute);
                var constructorInfo = methodAttribute.GetConstructor(stringType);
                var attributeBuilder = new CustomAttributeBuilder(constructorInfo, new object[] { methodData.XmlRpcName });
                methodBuilder.SetCustomAttribute(attributeBuilder);


                var ilGenerator = methodBuilder.GetILGenerator();
                var argValues = ilGenerator.DeclareLocal(typeof(object[]));
                ilGenerator.Emit(OpCodes.Ldc_I4, argCount);
                ilGenerator.Emit(OpCodes.Newarr, typeof(object));
                ilGenerator.Emit(OpCodes.Stloc, argValues);

                for (int argLoad = 0; argLoad < argCount; argLoad++)
                {
                    ilGenerator.Emit(OpCodes.Ldloc, argValues);
                    ilGenerator.Emit(OpCodes.Ldc_I4, argLoad);
                    ilGenerator.Emit(OpCodes.Ldarg, argLoad + 1);

                    var parameterInfos = parameters[argLoad];
                    var paramTypeName = parameterInfos.ParameterType.AssemblyQualifiedName;
                    paramTypeName = paramTypeName.Replace("&", "");
                    var paramType = Type.GetType(paramTypeName);

                    if (paramType.IsValueType)
                        ilGenerator.Emit(OpCodes.Box, paramType);

                    ilGenerator.Emit(OpCodes.Stelem_Ref);
                }

                var asyncCallback = ilGenerator.DeclareLocal(typeof(AsyncCallback));
                if (argCount < paramCount)
                {
                    ilGenerator.Emit(OpCodes.Ldarg, argCount + 1);
                    ilGenerator.Emit(OpCodes.Stloc, asyncCallback);
                }

                var objValue = ilGenerator.DeclareLocal(typeof(object));
                if (argCount < (paramCount - 1))
                {
                    ilGenerator.Emit(OpCodes.Ldarg, argCount + 2);
                    ilGenerator.Emit(OpCodes.Stloc, objValue);
                }

                var invokeTypes = new Type[]
                {
                    typeof(MethodInfo),
                    typeof(object[]),
                    typeof(object),
                    typeof(AsyncCallback),
                    typeof(object)
                };

                var invokeMethod = typeof(XmlRpcClientProtocol).GetMethod("BeginInvoke", invokeTypes);
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Call, typeof(MethodBase).GetMethod("GetCurrentMethod"));
                ilGenerator.Emit(OpCodes.Castclass, typeof(MethodInfo));
                ilGenerator.Emit(OpCodes.Ldloc, argValues);
                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldloc, asyncCallback);
                ilGenerator.Emit(OpCodes.Ldloc, objValue);
                ilGenerator.Emit(OpCodes.Call, invokeMethod);

                ilGenerator.Emit(OpCodes.Ret);
            }
        }

        static void BuildEndMethods(TypeBuilder tb, MethodData[] methods)
        {
            LocalBuilder returnValue = null;
            LocalBuilder tempReturnValue = null;

            foreach (var methodData in methods)
            {
                var methodInfo = methodData.MethodInfo;
                var argTypes = new Type[] { typeof(IAsyncResult) };
                var methodBuilder = tb.DefineMethod(methodInfo.Name, MethodAttributes.Public | MethodAttributes.Virtual, methodInfo.ReturnType, argTypes);

                var ilGenerator = methodBuilder.GetILGenerator();

                if (typeof(void) != methodInfo.ReturnType)
                {
                    tempReturnValue = ilGenerator.DeclareLocal(typeof(object));
                    returnValue = ilGenerator.DeclareLocal(methodInfo.ReturnType);
                }

                var invokeTypes = new Type[] { typeof(IAsyncResult), typeof(Type) };
                var invokeMethod = typeof(XmlRpcClientProtocol).GetMethod("EndInvoke", invokeTypes);
                var getTypeTypes = new Type[] { typeof(string) };
                var getTypeMethod = typeof(Type).GetMethod("GetType", getTypeTypes);

                ilGenerator.Emit(OpCodes.Ldarg_0);
                ilGenerator.Emit(OpCodes.Ldarg_1);
                ilGenerator.Emit(OpCodes.Ldstr, methodInfo.ReturnType.AssemblyQualifiedName);
                ilGenerator.Emit(OpCodes.Call, getTypeMethod);
                ilGenerator.Emit(OpCodes.Call, invokeMethod);

                if (typeof(void) != methodInfo.ReturnType)
                {
                    var retIsNull = ilGenerator.DefineLabel();
                    ilGenerator.Emit(OpCodes.Stloc, tempReturnValue);
                    ilGenerator.Emit(OpCodes.Ldloc, tempReturnValue);
                    ilGenerator.Emit(OpCodes.Brfalse, retIsNull);
                    ilGenerator.Emit(OpCodes.Ldloc, tempReturnValue);

                    if (true == methodInfo.ReturnType.IsValueType)
                    {
                        ilGenerator.Emit(OpCodes.Unbox, methodInfo.ReturnType);
                        ilGenerator.Emit(OpCodes.Ldobj, methodInfo.ReturnType);
                    }
                    else
                    {
                        ilGenerator.Emit(OpCodes.Castclass, methodInfo.ReturnType);
                    }

                    ilGenerator.Emit(OpCodes.Stloc, returnValue);
                    ilGenerator.MarkLabel(retIsNull);
                    ilGenerator.Emit(OpCodes.Ldloc, returnValue);
                }
                else
                {
                    ilGenerator.Emit(OpCodes.Pop);
                }

                ilGenerator.Emit(OpCodes.Ret);
            }
        }

        static void BuildConstructor(TypeBuilder typeBuilder, Type baseType, string url)
        {
            var ctorInfo = baseType.GetConstructor(Type.EmptyTypes);
            var ctorBuilder = typeBuilder.DefineConstructor(
                MethodAttributes.Public |
                MethodAttributes.SpecialName |
                MethodAttributes.RTSpecialName |
                MethodAttributes.HideBySig,
                CallingConventions.Standard,
                Type.EmptyTypes);

            if (!string.IsNullOrWhiteSpace(url))
            {
                var urlAttr = typeof(XmlRpcUrlAttribute);
                var oneString = new[] { typeof(string) };
                var constructor = urlAttr.GetConstructor(oneString);
                var attributeBuilder = new CustomAttributeBuilder(constructor, new object[] { url });

                typeBuilder.SetCustomAttribute(attributeBuilder);
            }

            var ilgen = ctorBuilder.GetILGenerator();
            ilgen.Emit(OpCodes.Ldarg_0);
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

        static MethodData[] GetXmlRpcMethods(Type serviceType)
        {
            var xmlRpcMethodInfos = new List<MethodData>();
            foreach (var methodInfo in SystemHelper.GetMethods(serviceType))
            {
                var xmlRpcName = GetXmlRpcMethodName(methodInfo);
                if (string.IsNullOrWhiteSpace(xmlRpcName))
                    continue;

                var parameterInfos = methodInfo.GetParameters();
                var hasParamsParameter = Attribute.IsDefined(parameterInfos[^1], typeof(ParamArrayAttribute));
                var methodData = new MethodData(methodInfo, xmlRpcName, hasParamsParameter);

                xmlRpcMethodInfos.Add(methodData);
            }

            return xmlRpcMethodInfos.ToArray();
        }

        static string GetXmlRpcMethodName(MethodInfo methodInfo)
        {
            var attribute = Attribute.GetCustomAttribute(methodInfo, typeof(XmlRpcMethodAttribute));
            if (attribute is XmlRpcMethodAttribute xmlAttribute)
                return string.IsNullOrWhiteSpace(xmlAttribute.Method) ? methodInfo.Name : xmlAttribute.Method;

            return null;
        }

        static MethodData[] GetXmlRpcBeginMethods(Type serviceType)
        {
            const string beginStart = "Begin";

            var beginMethods = new List<MethodData>();
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

            return beginMethods.ToArray();
        }

        static MethodData[] GetXmlRpcEndMethods(Type serviceType)
        {
            var endMethods = new List<MethodData>();

            var attributeMethods = serviceType.GetMethods().Where(m => Attribute.GetCustomAttribute(m, typeof(XmlRpcEndAttribute)) is XmlRpcEndAttribute);
            foreach (var methodInfo in attributeMethods)
            {
                var parameterInfo = methodInfo.GetParameters();
                if (parameterInfo.Length != 1 || parameterInfo[0].ParameterType == typeof(IAsyncResult))
                    throw new Exception($"method {methodInfo.Name} has invalid signature for end method");

                endMethods.Add(new MethodData(methodInfo, string.Empty, false));
            }

            return endMethods.ToArray();
        }
    }
}