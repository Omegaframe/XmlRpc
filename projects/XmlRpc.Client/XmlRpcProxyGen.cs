using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Reflection.Emit;
using XmlRpc.Client.Attributes;
using XmlRpc.Client.Exceptions;

namespace XmlRpc.Client
{
    public static class XmlRpcProxyGen
    {
        static readonly Hashtable _types = new Hashtable();

        public static T Create<T>(HttpClient client) where T : IXmlRpcProxy
        {
            return (T)Create(typeof(T), client);
        }

        public static object Create(Type serviceType, HttpClient client)
        {
            if (!serviceType.IsInterface)
                throw new XmlRpcServiceIsNoInterfaceException($"Requested Type {serviceType.Name} is no interface but has to be one.");

            if (!typeof(IXmlRpcProxy).IsAssignableFrom(serviceType))
                throw new XmlRpcServiceInterfaceNotImplementedException($"Requested Type {serviceType.Name} does not implement required interface {nameof(IXmlRpcProxy)}");

            var proxyType = (Type)_types[serviceType] ?? BuildServiceType(serviceType);
            return Activator.CreateInstance(proxyType, new[] { client });
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
            var methods = GetXmlRpcMethods(serviceType);

            var assemblyName = new AssemblyName { Name = name };

            if (access == AssemblyBuilderAccess.Run)
                assemblyName.Version = serviceType.Assembly.GetName().Version;

            var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, access);
            var moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);
            var typeBuilder = moduleBuilder.DefineType(typeName, TypeAttributes.Class | TypeAttributes.Sealed | TypeAttributes.Public, typeof(XmlRpcClient), new Type[] { serviceType });

            BuildConstructor(typeBuilder, typeof(XmlRpcClient));
            BuildMethods(typeBuilder, methods);

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
            var invokeMethod = typeof(XmlRpcClient).GetMethod("Invoke", invokeTypes);
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

        static void BuildConstructor(TypeBuilder typeBuilder, Type baseType)
        {
            var ctorInfo = baseType.GetConstructor(new[] { typeof(HttpClient) });
            var ctorBuilder = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.HasThis, new[] { typeof(HttpClient) });

            var conObj = typeof(object).GetConstructor(new Type[0]);

            var ilgen = ctorBuilder.GetILGenerator();
            ilgen.Emit(OpCodes.Ldarg_0);
            ilgen.Emit(OpCodes.Call, conObj);
            ilgen.Emit(OpCodes.Ldarg_0);
            ilgen.Emit(OpCodes.Ldarg_1);
            ilgen.Emit(OpCodes.Call, ctorInfo);
            ilgen.Emit(OpCodes.Ret);
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
    }
}