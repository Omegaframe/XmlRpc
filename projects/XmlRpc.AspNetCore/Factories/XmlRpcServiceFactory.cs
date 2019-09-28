using System;
using Microsoft.Extensions.DependencyInjection;

namespace XmlRpc.AspNetCore.Factories
{
    internal class XmlRpcServiceFactory : IXmlRpcServiceFactory
    {
        readonly IServiceProvider _serviceProvider;

        public XmlRpcServiceFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public XmlRpcService CreateService<TService>() where TService : XmlRpcService 
        {
            return ActivatorUtilities.CreateInstance<TService>(_serviceProvider);
        }
    }
}