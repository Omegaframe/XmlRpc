using System;
using System.Threading.Tasks;
using XmlRpc.AspNetCore.Adapter;
using XmlRpc.AspNetCore.Factories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;

namespace XmlRpc.AspNetCore.Routing
{
    internal class ServiceRouteBuilder : IServiceRouteBuilder
    {
        readonly IRouteBuilder _routes;
        readonly IXmlRpcServiceFactory _serviceFactory;

        public ServiceRouteBuilder(IRouteBuilder routes, IXmlRpcServiceFactory serviceFactory) 
        {
            _routes = routes;
            _serviceFactory = serviceFactory;
        }

        public IServiceRouteBuilder MapService<TService>(string template) where TService : XmlRpcService
        {
            _routes.MapRoute(template, DelegateRpcServiceRequest<TService>);

            return this;
        }

        Task DelegateRpcServiceRequest<TService>(HttpContext context) where TService : XmlRpcService
        {
            var service = _serviceFactory.CreateService<TService>();
            return service.HandleHttpRequestAsync(context);
        }
    }
}