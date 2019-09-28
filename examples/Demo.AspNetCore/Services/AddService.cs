using System;
using Demo.Contracts;
using XmlRpc.AspNetCore;

namespace Demo.AspNetCore.Services
{
    public class AddService : XmlRpcService, IAddService
    {
        public int AddNumbers(int numberA, int numberB)
        {
            return numberA + numberB;
        }
    }
}
