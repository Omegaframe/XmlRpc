using System;
using XmlRpc.Client.DataTypes;

namespace XmlRpc.Client.Serializer
{
    static class ValueTypeExtensions
    {
        public static bool IsNoInteger(this Type valueType)
        {
            return valueType != null
                && valueType != typeof(object)
                && valueType != typeof(int)
                && valueType != typeof(int?)
                && valueType != typeof(XmlRpcInt);
        }

        public static bool IsNoLong(this Type valueType)
        {
            return valueType != null
                && valueType != typeof(object)
                && valueType != typeof(long)
                && valueType != typeof(long?);
        }

        public static bool IsNoString(this Type valueType)
        {
            return valueType != null
                && valueType != typeof(string)
                && valueType != typeof(object);
        }

        public static bool IsNoBoolean(this Type valueType)
        {
            return valueType != null
                && valueType != typeof(object)
                && valueType != typeof(bool)
                && valueType != typeof(bool?)
                && valueType != typeof(XmlRpcBoolean);
        }

        public static bool IsNoDouble(this Type valueType)
        {
            return valueType != null
                && valueType != typeof(object)
                && valueType != typeof(double)
                && valueType != typeof(double?)
                && valueType != typeof(XmlRpcDouble);
        }

        public static bool IsNoDateTime(this Type valueType)
        {
            return valueType != null
                && valueType != typeof(object)
                && valueType != typeof(DateTime)
                && valueType != typeof(DateTime?)
                && valueType != typeof(XmlRpcDateTime);
        }

        public static bool IsNoByteArray(this Type valueType)
        {
            return valueType != null
                && valueType != typeof(object)
                && valueType != typeof(byte[]);
        }
    }
}
