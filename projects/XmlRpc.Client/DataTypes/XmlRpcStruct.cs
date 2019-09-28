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
            {
                throw new ArgumentException("XmlRpcStruct key must be a string.");
            }
            if (XmlRpcServiceInfo.GetXmlRpcType(value.GetType())
                == XmlRpcType.tInvalid)
            {
                throw new ArgumentException(String.Format(
                  "Type {0} cannot be mapped to an XML-RPC type", value.GetType()));
            }
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
                {
                    throw new ArgumentException("XmlRpcStruct key must be a string.");
                }
                if (XmlRpcServiceInfo.GetXmlRpcType(value.GetType())
                    == XmlRpcType.tInvalid)
                {
                    throw new ArgumentException(String.Format(
                      "Type {0} cannot be mapped to an XML-RPC type", value.GetType()));
                }
                base[key] = value;
                _keys.Add(key);
                _values.Add(value);
            }
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() != typeof(XmlRpcStruct))
                return false;
            XmlRpcStruct xmlRpcStruct = (XmlRpcStruct)obj;
            if (this.Keys.Count != xmlRpcStruct.Count)
                return false;
            foreach (String key in this.Keys)
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
            return new XmlRpcStruct.Enumerator(_keys, _values);
        }

        public override ICollection Keys
        {
            get
            {
                return _keys;
            }
        }

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

        public override ICollection Values
        {
            get
            {
                return _values;
            }
        }

        class Enumerator : IDictionaryEnumerator
        {
            readonly ArrayList _keys;
            readonly ArrayList _values;
            int _index;

            public Enumerator(ArrayList keys, ArrayList values)
            {
                _keys = keys;
                _values = values;
                _index = -1;
            }

            public void Reset()
            {
                _index = -1;
            }

            public object Current
            {
                get
                {
                    CheckIndex();
                    return new DictionaryEntry(_keys[_index], _values[_index]);
                }
            }

            public bool MoveNext()
            {
                _index++;
                if (_index >= _keys.Count)
                    return false;
                else
                    return true;
            }

            public DictionaryEntry Entry
            {
                get
                {
                    CheckIndex();
                    return new DictionaryEntry(_keys[_index], _values[_index]);
                }
            }

            public object Key
            {
                get
                {
                    CheckIndex();
                    return _keys[_index];
                }
            }

            public object Value
            {
                get
                {
                    CheckIndex();
                    return _values[_index];
                }
            }

            void CheckIndex()
            {
                if (_index < 0 || _index >= _keys.Count)
                    throw new InvalidOperationException(
                      "Enumeration has either not started or has already finished.");
            }
        }
    }
}