using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ToolKit.SerializedDictionary
{
    [System.Serializable]
    public struct SerializedKeyValuePair<TKey, TValue>
    {
        [SearchableEnum]
        public TKey Key;
        public TValue Value;

        public SerializedKeyValuePair(TKey key, TValue value)
        {
            Key = key;
            Value = value;
        }
    }
}
