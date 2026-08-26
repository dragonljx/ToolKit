using ToolKit.SerializedDictionary.Editor;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

internal class DefaultListState : ListState
{
    public override int ListSize => Drawer.ListProperty.arraySize;


    public DefaultListState(
        SerializedDictionaryInstanceDrawer serializedDictionaryDrawer)
        : base(serializedDictionaryDrawer)
    {
    }


    public override void OnEnter()
    {
        Drawer.ReorderableList.draggable = true;
    }


    public override void OnExit()
    {
    }


    public override ListState OnUpdate()
    {
        if (Drawer.SearchText.Length > 0)
            return Drawer.SearchState;

        return this;
    }


    public override void DrawElement(
        Rect rect,
        SerializedProperty property,
        DisplayType displayType)
    {
        SerializedDictionaryInstanceDrawer.DrawElement(
            rect,
            property,
            displayType);
    }


    public override SerializedProperty GetPropertyAtIndex(int index)
    {
        if (index < 0 ||
            index >= Drawer.ListProperty.arraySize)
        {
            return null;
        }


        return Drawer.ListProperty.GetArrayElementAtIndex(index);
    }


    public override void RemoveElementAt(int index)
    {
        if (index < 0 ||
           index >= Drawer.ListProperty.arraySize)
            return;


        Drawer.ListProperty.DeleteArrayElementAtIndex(index);


        Drawer.ListProperty.serializedObject.ApplyModifiedProperties();


        Drawer.ListProperty.serializedObject.Update();
    }


    public override void InserElementAt(int index)
    {
        index = Mathf.Clamp(
            index,
            0,
            Drawer.ListProperty.arraySize);


        Drawer.ListProperty.InsertArrayElementAtIndex(index);


        Drawer.ListProperty.serializedObject.ApplyModifiedProperties();


        Drawer.ListProperty.serializedObject.Update();
    }
}