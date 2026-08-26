using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ToolKit.SerializedDictionary.Editor
{
    public abstract class KeyListGenerator : ScriptableObject
    {
        public abstract IEnumerable GetKeys(System.Type type);
    }
}