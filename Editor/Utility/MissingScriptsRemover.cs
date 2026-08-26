using System.Collections.Generic;
using System.Linq;
using ToolKit;
using UnityEditor;
using UnityEngine;

namespace ToolKit.Editor
{
    public static class FindMissingScriptsRecursively
    {
        private static bool _isProcessing = false;
        private const int MaxNamesToShow = 10;

        [MenuItem(ToolGlobal.AssetMenuName + "删除缺少的脚本")]
        [MenuItem(ToolGlobal.GameObjectMenuName + "删除缺少的脚本")]
        private static void FindAndRemoveMissingInSelected()
        {
            if (_isProcessing)
                return;

            _isProcessing = true;
            try
            {
                var selected = Selection.gameObjects
                    .SelectMany(go => go.GetComponentsInChildren<Transform>(true))
                    .Select(t => t.gameObject)
                    .Distinct();

                var prefabs = new HashSet<Object>();
                var affectedGameObjects = new List<GameObject>();
                var removedComponentsCount = 0;
                var removedGameObjectsCount = 0;
                var didModifyPrefabAsset = false;

                foreach (var gameObject in selected)
                {
                    var count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject);
                    if (count == 0)
                        continue;

                    if (PrefabUtility.IsPartOfAnyPrefab(gameObject))
                    {
                        RecursivePrefabSource(gameObject, prefabs, ref removedComponentsCount,
                            ref removedGameObjectsCount, ref didModifyPrefabAsset);
                        count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(gameObject);
                        if (count == 0)
                            continue;
                    }

                    Undo.RegisterCompleteObjectUndo(gameObject, "Remove missing scripts");
                    GameObjectUtility.RemoveMonoBehavioursWithMissingScript(gameObject);
                    affectedGameObjects.Add(gameObject);
                    removedComponentsCount += count;
                    removedGameObjectsCount += 1;
                }

                if (removedGameObjectsCount == 0)
                {
                    Debug.Log("<color=#32a852>There is no selected game objects with missing scripts.</color>");
                }
                else
                {
                    var names = affectedGameObjects.Select(go => go.name).ToList();
                    string namesStr = names.Count <= MaxNamesToShow
                        ? string.Join(", ", names)
                        : string.Join(", ", names.Take(MaxNamesToShow)) + $" ...等共{names.Count}个";

                    Debug.Log(
                        $"<color=#c8d649>Removed {removedComponentsCount} missing scripts from {removedGameObjectsCount} game objects.</color>" +
                        $"\nGame objects: <color=#c8d649>{namesStr}.</color>");

                    // Prefab Source 上的修改需要显式保存,否则只是标记 dirty,不会立刻落盘
                    if (didModifyPrefabAsset)
                        AssetDatabase.SaveAssets();
                }
            }
            finally
            {
                _isProcessing = false;
            }
        }

        [MenuItem(ToolGlobal.AssetMenuName + "删除缺少的脚本", true)]
        [MenuItem(ToolGlobal.GameObjectMenuName + "删除缺少的脚本", true)]
        private static bool ValidateFindAndRemoveMissingInSelected()
        {
            return Selection.gameObjects.Length > 0;
        }

        private static void RecursivePrefabSource(GameObject instance, HashSet<Object> prefabs,
            ref int removedComponentsCount, ref int removedGameObjectsCount, ref bool didModifyPrefabAsset)
        {
            var source = PrefabUtility.GetCorrespondingObjectFromSource(instance);
            if (source == null || !prefabs.Add(source))
                return;

            RecursivePrefabSource(source, prefabs, ref removedComponentsCount, ref removedGameObjectsCount,
                ref didModifyPrefabAsset);

            var count = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(source);
            if (count > 0)
            {
                Undo.RegisterCompleteObjectUndo(source, "Remove missing scripts");
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(source);
                EditorUtility.SetDirty(source);
                removedComponentsCount += count;
                removedGameObjectsCount += 1;
                didModifyPrefabAsset = true;
            }
        }
    }
}