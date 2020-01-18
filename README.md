# Omegaframe.XmlRpc
.Netstandard implementation of XML-RPC. Provides client and server libraries to work with XML-RPC in your .Net (core) applications.

Everything here is released under MIT license. See [License information](https://github.com/Omegaframe/XmlRpc/blob/master/LICENSE.md) to get further information.

## Packages
There are four nuget packages available on [nuget.org](https://www.nuget.org/):
-  [Omegaframe.XmlRpc.Client](https://www.nuget.org/packages/Omegaframe.XmlRpc.Client/)
-  [Omegaframe.XmlRpc.Server](https://www.nuget.org/packages/Omegaframe.XmlRpc.Server/)
-  [Omegaframe.XmlRpc.Kestrel](https://www.nuget.org/packages/Omegaframe.XmlRpc.Kestrel/)
-  [Omegaframe.XmlRpc.Listener](https://www.nuget.org/packages/Omegaframe.XmlRpc.Listener/)

## Omegaframe.XmlRpc.Client
This package contains everything you need to connect to a Xml-Rpc Server. Building a client is quite simple:
1. Create a contract-interface containing the mehtods your want to call remotely. Ensure to set the XmlRpcMethod attribute in order to mark a method as remote procedure. 
```
public interface IAddService
{
  [XmlRpcMethod("Demo.addNumbers")]
  int AddNumbers(int numberA, int numberB);
}
```

2. Create a client interface that extends the IXmlRpcClient interface and the contract you created earlier.
```   
public interface IAddServiceClient : IXmlRpcClient, IAddService { }
```

3. Set up your connection using a normal HttpClient. This is where you would set up host, port, credentials, certifactes etc.
```
var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5678/xmlrpc") };
```

4. Set up your Xml-Rpc configuration. You can stick to the default settings or configure what encoding to use, how to handle missing members, non-standard behaeviour, etc.
```
var config = new SerializerConfig();
```

5. Use the XmlRpcClientBuilder to create an instance of your client.
```
var xmlRpcClient = XmlRpcClientBuilder.Create<IAddServiceClient>(httpClient, config);
```

6. Call the methods and invoke remote procedure calls. that's it.
```
var result = xmlRpcClient.AddNumbers(3, 4);
```

Whole code would look like this:
```
var httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5678/xmlrpc") };            
var config = new SerializerConfig();
var xmlRpcClient = XmlRpcClientBuilder.Create<IAddServiceClient>(httpClient, config);

var result = xmlRpcClient.AddNumbers(3, 4);
Console.WriteLine($"Received result: {result}");
```

## Omegaframe.XmlRpc.Server
This package contains everything you need to implement XmlRpc within your own web-/apiserver. The XmlRpc stuff is handled here, all you need to do is to provide the required interfaces and handle them inside your server application. See the source of [Kestrel-Middleware](https://github.com/Omegaframe/XmlRpc/tree/master/projects/XmlRpc.Kestrel) or [HttpListener](https://github.com/Omegaframe/XmlRpc/tree/master/projects/XmlRpc.Listener) for examples on how to integrade this package in your webserver code. 
*if you're not building your own webserver and want to support xml-rpc this is not the package to choose. use the kestrel / listener implementations of this instead*

## Omegaframe.XmlRpc.Kestrel
This package contains the kestrel middleware implementation of Omegaframe.XmlRpc.Server. If you are working with .Net Core and / or Kestrel and want to allow Xml remote procedure calls, this is the package you choose. Using the middleware is as easy as it can be:
1. Create a contract interface that defines the method you want to make remotely callable. Remember to set the XmlRpcMethod attribute. 
 ```
// if you're providing a client as well, ensure server and client use the same interface.
public interface IAddService
{  
  [XmlRpcMethod("Demo.addNumbers")]
  int AddNumbers(int numberA, int numberB);
}
```

2. Create a class that extend XmlRpcService and implements your contract interface. This is the actual method that gets executed if a remote call is received.
```
internal class AddService : XmlRpcService, IAddService
{
  public int AddNumbers(int numberA, int numberB)
  {
    Console.WriteLine($"Received request to Demo.addNumbers. Parameters: [{numberA}, {numberB}]");
    return numberA + numberB;
  }
}
```

3. Build a kestrel webhost with enabled XmlRpc and set the route on which it will be availabe. that's it.
```
 var host = new WebHostBuilder()
               .UseKestrel(o => o.Listen(IPAddress.Any, 5678))         
               .ConfigureServices(s => s.AddXmlRpc(new AddService()))   // register xml-rpc service class
               .Configure(c => c.UseXmlRpc<AddService>("/xmlrpc"))      // map route to service
               .Build();
```

## Omegaframe.XmlRpc.Listener
This package contains the HttpListener implementation of Omegaframe.XmlRpc.Server. While this is quite simple, it might be all you need to provide remote procedures in an application wihtout the need of including a whole webserver. Follow these steps to use XmlRpc:
1. Create a contract interface that defines the method you want to make remotely callable. Remember to set the XmlRpcMethod attribute. 
 ```
 // if you're providing a client as well, ensure server and client use the same interface.
public interface IAddService
{  
  [XmlRpcMethod("Demo.addNumbers")]
  int AddNumbers(int numberA, int numberB);
}
```

2. Create a class that extend XmlRpcService and implements your contract interface. This is the actual method that gets executed if a remote call is received.
```
internal class AddService : XmlRpcService, IAddService
{
  public int AddNumbers(int numberA, int numberB)
  {
    Console.WriteLine($"Received request to Demo.addNumbers. Parameters: [{numberA}, {numberB}]");
    return numberA + numberB;
  }
}
```

3. Create a HttpListener with your desired configuration and handle incoming requests. Make sure to create an instance of your service class of your own and give it your HttpListenerContext to handle the XmlRpc Requests.
```
var listener = new HttpListener();
listener.Prefixes.Add("http://localhost:5678/xmlrpc/");
listener.Start();

while (true)
{
  var context = listener.GetContext();
  
  var service = new AddService();
  service.ProcessRequest(context);
}
```

## Examples
There are some [Example Projects](https://github.com/Omegaframe/XmlRpc/tree/master/examples) you might want to look into in order to get an idea on how to use this libraries.

## Thank you
Forked from https://github.com/Horizon0156/XmlRpc

Thanks to Horizon0156 for the initial work
