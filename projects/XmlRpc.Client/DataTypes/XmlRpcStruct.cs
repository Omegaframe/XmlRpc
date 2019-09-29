using System;
using System.Collections;
using XmlRpc.Client.Model;

namespace XmlRpc.Client.DataTypes
{
    public class XmlRpcStruct : Hashtable
    {
        readonly ArrayList _keys = new ArrayList();
        readonly ArrayList _values = new ArrayList();

        public override void Add(object key, object value)
        {
            if (!(key is string))
                throw new ArgumentException("XmlRpcStruct key must be a string.");

            if (XmlRpcServiceInfo.GetXmlRpcType(value.GetType()) == XmlRpcType.tInvalid)
                throw new ArgumentException($"Type {value.GetType()} cannot be mapped to an XML-RPC type");

            base.Add(key, value);
            _keys.Add(key);
            _values.Add(value);
        }

        public override object this[object key]
        {
            get
            {
                return base[key];
            }
            set
            {
                if (!(key is string))
                    throw new ArgumentException("XmlRpcStruct key must be a string.");

                if (XmlRpcServiceInfo.GetXmlRpcType(value.GetType()) == XmlRpcType.tInvalid)
                    throw new ArgumentException($"Type {value.GetType()} cannot be mapped to an XML-RPC type");

                base[key] = value;
                _keys.Add(key);
                _values.Add(value);
            }
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() != typeof(XmlRpcStruct))
                return false;

            var xmlRpcStruct = (XmlRpcStruct)obj;
            if (Keys.Count != xmlRpcStruct.Count)
                return false;

            foreach (var key in Keys)
            {
                if (!xmlRpcStruct.ContainsKey(key))
                    return false;

                if (!this[key].Equals(xmlRpcStruct[key]))
                    return false;
            }

            return true;
        }

        public override int GetHashCode()
        {
            int hash = 0;
            foreach (object obj in Values)
            {
                hash ^= obj.GetHashCode();
            }
            return hash;
        }

        public override void Clear()
        {
            base.Clear();
            _keys.Clear();
            _values.Clear();
        }

        public new IDictionaryEnumerator GetEnumerator()
        {
            return new Enumerator(_keys, _values);
        }

        public override ICollection Keys => _keys;

        public override void Remove(object key)
        {
            base.Remove(key);

            int idx = _keys.IndexOf(key);
            if (idx >= 0)
            {
                _keys.RemoveAt(idx);
                _values.RemoveAt(idx);
            }
        }

        public override ICollection Values => _values;
    }
}