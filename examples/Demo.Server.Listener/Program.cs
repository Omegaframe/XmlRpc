using System;
using System.Net;

namespace XmlRpc.ServerDemo
{
    class Program
    {
        static void Main()
        {
            // create a HttpListener that fits your needs
            var listener = new HttpListener();
            listener.Prefixes.Add("http://127.0.0.1:5678/xmlrpc/");
            listener.Start();

            Console.WriteLine("Started Demo service. Press CTRL+C to exit..." );
            while (true)
            {
                var context = listener.GetContext();

                // create an instance of your service an call ProcessRequest with you listeners' context
                var service = new AddService();
                service.ProcessRequest(context);
            }
        }
    }
}
