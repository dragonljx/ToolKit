using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

#if UNITY_EDITOR

using UnityEditor;

#endif

using UnityEngine;

namespace ToolKit
{
    [Serializable]
    public class SerializedDictionary<TKey, TValue> : ISerializationCallbackReceiver, IDictionary<TKey, TValue>
    {
        [SerializeField] private List<SerializableKeyValuePair> list = new List<SerializableKeyValuePair>();

        [Serializable]
        private struct SerializableKeyValuePair
        {
            public TKey Key;
            public TValue Value;

            public SerializableKeyValuePair(TKey key, TValue value)
            {
                Key = key;
                Value = value;
            }
        }

        private Dictionary<TKey, int> _keyPositions = new Dictionary<TKey, int>();

        // 构造函数
        public SerializedDictionary()
        {
        }

        // 序列化回调
        public void OnBeforeSerialize()
        { }

        public void OnAfterDeserialize()
        {
            RebuildKeyPositions();
        }

        private void RebuildKeyPositions()
        {
            _keyPositions.Clear();
            for (int i = 0; i < list.Count; i++)
            {
                var key = list[i].Key;
                if (!_keyPositions.ContainsKey(key))
                    _keyPositions[key] = i;
                else
                    Debug.LogWarning($"SerializedDictionary contains duplicate key: {key}");
            }
        }

        #region IDictionary<TKey, TValue>

        public TValue this[TKey key]
        {
            get
            {
                if (_keyPositions.TryGetValue(key, out var index))
                    return list[index].Value;
                throw new KeyNotFoundException($"Key '{key}' not found.");
            }
            set
            {
                if (_keyPositions.TryGetValue(key, out var index))
                {
                    list[index] = new SerializableKeyValuePair(key, value);
                }
                else
                {
                    _keyPositions[key] = list.Count;
                    list.Add(new SerializableKeyValuePair(key, value));
                }
            }
        }

        public ICollection<TKey> Keys => list.Select(kv => kv.Key).ToArray();
        public ICollection<TValue> Values => list.Select(kv => kv.Value).ToArray();

        public void Add(TKey key, TValue value)
        {
            if (_keyPositions.ContainsKey(key))
                throw new ArgumentException($"An element with the same key already exists: {key}");
            _keyPositions[key] = list.Count;
            list.Add(new SerializableKeyValuePair(key, value));
        }

        public bool ContainsKey(TKey key) => _keyPositions.ContainsKey(key);

        public bool Remove(TKey key)
        {
            if (_keyPositions.TryGetValue(key, out var index))
            {
                _keyPositions.Remove(key);
                list.RemoveAt(index);
                // 更新后续元素索引
                for (int i = index; i < list.Count; i++)
                    _keyPositions[list[i].Key] = i;
                return true;
            }
            return false;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            if (_keyPositions.TryGetValue(key, out var index))
            {
                value = list[index].Value;
                return true;
            }
            value = default;
            return false;
        }

        #endregion IDictionary<TKey, TValue>

        #region ICollection<KeyValuePair<TKey, TValue>>

        public int Count => list.Count;
        public bool IsReadOnly => false;

        public void Add(KeyValuePair<TKey, TValue> kvp) => Add(kvp.Key, kvp.Value);

        public void Clear()
        {
            list.Clear();
            _keyPositions.Clear();
        }

        public bool Contains(KeyValuePair<TKey, TValue> kvp)
        {
            return TryGetValue(kvp.Key, out var value) &&
                   EqualityComparer<TValue>.Default.Equals(value, kvp.Value);
        }

        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));
            if (arrayIndex < 0 || arrayIndex > array.Length)
                throw new ArgumentOutOfRangeException(nameof(arrayIndex));
            if (array.Length - arrayIndex < list.Count)
                throw new ArgumentException("Destination array is too small.");

            for (int i = 0; i < list.Count; i++)
                array[arrayIndex + i] = new KeyValuePair<TKey, TValue>(list[i].Key, list[i].Value);
        }

        public bool Remove(KeyValuePair<TKey, TValue> kvp)
        {
            return Remove(kvp.Key);
        }

        #endregion ICollection<KeyValuePair<TKey, TValue>>

        #region IEnumerable<KeyValuePair<TKey, TValue>>

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            return list.Select(kv => new KeyValuePair<TKey, TValue>(kv.Key, kv.Value)).GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion IEnumerable<KeyValuePair<TKey, TValue>>
    }

#if UNITY_EDITOR

    [CustomPropertyDrawer(typeof(SerializedDictionary<,>), true)]
    public class SerializedDictionaryDrawer : PropertyDrawer
    {
        private SerializedProperty GetListProperty(SerializedProperty property)
        {
            return property.FindPropertyRelative("list");
        }

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.PropertyField(position, GetListProperty(property), label, true);
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            return EditorGUI.GetPropertyHeight(GetListProperty(property), true);
        }
    }

#endif
}