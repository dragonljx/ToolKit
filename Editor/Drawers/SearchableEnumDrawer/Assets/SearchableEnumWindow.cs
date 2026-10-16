using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ToolKit.Editor
{
    public class SearchableEnumWindow : EditorWindow
    {
        [SerializeField]
        private VisualTreeAsset _visualTreeAsset;

        [SerializeField]
        private StyleSheet _styleSheet;

        private SerializedProperty _serializedProperty;
        private string _propertyPath;
        private ToolbarSearchField _searchField;
        private ListView _enumList;
        private int _selectedIndex;
        private List<string> _enumNames;
        private List<int> _filteredIndices = new();
        public static void Show(SerializedProperty property,Rect buttonRect,Vector2 windowsSize)
        {
            var window = CreateInstance<SearchableEnumWindow>();
            window._serializedProperty = property;
            window._enumNames = property.enumNames.ToList();
            window._propertyPath = property.propertyPath;
            window._selectedIndex = property.enumValueIndex;
            window.ShowAsDropDown(buttonRect, windowsSize);
        }

        private void CreateGUI()
        {
            if (_visualTreeAsset == null)
            {
                Debug.LogError("SearchableEnumWindow VisualTreeAsset 未设置");
                return;
            }

            _visualTreeAsset.CloneTree(rootVisualElement);

            if (_styleSheet != null)
            {
                rootVisualElement.styleSheets.Add(_styleSheet);
            }

            _searchField =rootVisualElement.Q<ToolbarSearchField>("enumSearchField");

            _enumList = rootVisualElement.Q<ListView>("enumListView");
  
            Initialize();

        }
        private void Initialize()
        {
            _searchField.RegisterValueChangedCallback(OnSearchChanged);
            _enumList.itemsSource = _filteredIndices;
            _enumList.fixedItemHeight = EditorGUIUtility.singleLineHeight;
            _enumList.selectedIndex = _selectedIndex;
            _enumList.onSelectionChange += OnSelectionChanged;

            _enumList.makeItem = () =>
            {
                return new Label();
            };

            _enumList.bindItem = (element, index) =>
            {
                var label = (Label)element;

                int enumIndex = _filteredIndices[index];
 
                label.text = _enumNames[enumIndex];
            };


            _enumList.schedule.Execute(() =>
            {
                _enumList.ScrollToItem(_selectedIndex);
            });

            RefreshList(string.Empty);

            _searchField.Focus();
        }

        private void OnSelectionChanged(IEnumerable<object> enumerable)
        {
            int? firstItem = (int?)enumerable.FirstOrDefault();
 
            if (firstItem == null)
                return;

            _serializedProperty.serializedObject.Update();

            _serializedProperty.enumValueIndex = (int)firstItem;

            _serializedProperty.serializedObject.ApplyModifiedProperties();

            Close();
        }
        private void RefreshList(string searchText)
        {
            _filteredIndices.Clear();

            for (int i = 0; i < _enumNames.Count; i++)
            {
                if (string.IsNullOrEmpty(searchText) ||
                    _enumNames[i].Contains(
                        searchText,
                        StringComparison.OrdinalIgnoreCase))
                {
                    _filteredIndices.Add(i);
                }
            }

            _enumList.RefreshItems();
        }
        private void OnSearchChanged(ChangeEvent<string> evt)
        {
            RefreshList(evt.newValue);
        }
    }
}