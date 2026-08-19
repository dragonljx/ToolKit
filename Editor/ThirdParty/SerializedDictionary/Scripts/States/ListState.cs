using UnityEditor;
using UnityEngine;

namespace ToolKit.SerializedDictionary.Editor
{
    internal abstract class ListState
    {
        public abstract int ListSize { get; }
        public virtual string NoElementsText => "List is Empty.";

        public readonly SerializedDictionaryInstanceDrawer Drawer;

        public ListState(SerializedDictionaryInstanceDrawer serializedDictionaryDrawer)
        {
            Drawer = serializedDictionaryDrawer;
        }

        public abstract SerializedProperty GetPropertyAtIndex(int index);
        public abstract ListState OnUpdate();
        public abstract void OnEnter();
        public abstract void OnExit();
        public abstract void DrawElement(Rect rect, SerializedProperty property, DisplayType displayType);
        public abstract void RemoveElementAt(int index);
        public abstract void InserElementAt(int index);

        /// <summary>
        /// 返回列表位置对应的真实数组索引。默认状态下两者一致，搜索状态下需要映射到搜索结果对应的数组位置。
        /// </summary>
        public virtual int GetRealArrayIndex(int index) => index;

        public virtual float GetHeightAtIndex(int index, bool drawKeyAsList, bool drawValueAsList)
        {
            return SerializedDictionaryInstanceDrawer.CalculateHeightOfElement(GetPropertyAtIndex(index), drawKeyAsList, drawValueAsList);
        }
    }
}