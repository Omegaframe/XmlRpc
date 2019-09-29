using System;
using System.Collections;

namespace XmlRpc.Client.Model
{
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
                throw new InvalidOperationException("Enumeration has either not started or has already finished.");
        }
    }
}
