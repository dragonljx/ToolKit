using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ToolKit.SerializedDictionary
{
    internal interface IKeyable
    {
        void RecalculateOccurences();
        IReadOnlyList<int> GetOccurences(object key);
        IEnumerable Keys { get; }
        int Count { get; }
        void AddKey(object key);
        void RemoveKey(object key);
        void RemoveAt(int index);
        object GetKeyAt(int index);
        void RemoveDuplicates();
    }
}
