using System;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
#endif
using UnityEngine;
using Object = UnityEngine.Object;

namespace ToolKits
{
    [Serializable]
    public class SceneSerialized : ISerializationCallbackReceiver
 {
#if UNITY_EDITOR
        // What we use in editor to select the scene
        [SerializeField] private Object sceneAsset;
        private bool IsValidSceneAsset
        {
            get
            {
                if (!sceneAsset) return false;

                return sceneAsset is SceneAsset;
            }
        }
#endif

        // This should only ever be set during serialization/deserialization!
        [SerializeField]
        private string scenePath = string.Empty;


        // Use this when you want to actually have the scene path
        public string ScenePath
        {
            get
            {
#if UNITY_EDITOR
                return GetScenePathFromAsset();
#else
            return scenePath;
#endif
            }
            set
            {
                scenePath = value;
#if UNITY_EDITOR
                sceneAsset = GetSceneAssetFromPath();
#endif
            }
        }
        
        [SerializeField]
        private string sceneName = string.Empty;
        public string SceneName
        {
            get
            {
#if UNITY_EDITOR
                return GetSceneNameFromAsset();
#else
            return sceneName;
#endif
            }
            set
            {
                sceneName = value;
#if UNITY_EDITOR
                sceneName = GetSceneNameFromPath();
#endif
            }
        }
        public static implicit operator string(SceneSerialized sceneReference)
        {
            return sceneReference.ScenePath;
        }

        // Called to prepare this data for serialization. Stubbed out when not in editor.
        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            HandleBeforeSerialize();
#endif
        }

        // Called to set up data for deserialization. Stubbed out when not in editor.
        public void OnAfterDeserialize()
        {
#if UNITY_EDITOR
            // We sadly cannot touch assetdatabase during serialization, so defer by a bit.
            EditorApplication.update += HandleAfterDeserialize;
#endif
        }

#if UNITY_EDITOR
        private SceneAsset GetSceneAssetFromPath()
        {
            return string.IsNullOrEmpty(scenePath) ? null : AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath);
        }

        private string GetScenePathFromAsset()
        {
            return sceneAsset == null ? string.Empty : AssetDatabase.GetAssetPath(sceneAsset);
        }
        private string GetSceneNameFromAsset()
        {
            return sceneAsset == null ? string.Empty :(sceneAsset as SceneAsset)?.name;
        }
        private string GetSceneNameFromPath()
        {
            return sceneAsset == null ? string.Empty : AssetDatabase.LoadAssetAtPath<SceneAsset>(scenePath).name;
        }
        private void HandleBeforeSerialize()
        {
            // Asset is invalid but have Path to try and recover from
            if (IsValidSceneAsset == false && string.IsNullOrEmpty(scenePath) == false &&string.IsNullOrEmpty(sceneName) == false)
            {
                sceneAsset = GetSceneAssetFromPath();
                if (sceneAsset == null)
                {
                    scenePath = string.Empty;
                    sceneName = string.Empty;
                }

                EditorSceneManager.MarkAllScenesDirty();
            }
            // Asset takes precendence and overwrites Path
            else
            {
                scenePath = GetScenePathFromAsset();
                sceneName = GetSceneNameFromAsset();
            }
        }

        private void HandleAfterDeserialize()
        {
            EditorApplication.update -= HandleAfterDeserialize;
            // Asset is valid, don't do anything - Path will always be set based on it when it matters
            if (IsValidSceneAsset) return;

            // Asset is invalid but have path to try and recover from
            if (string.IsNullOrEmpty(scenePath)||string.IsNullOrEmpty(sceneName)) return;

            sceneAsset = GetSceneAssetFromPath();
            // No asset found, path was invalid. Make sure we don't carry over the old invalid path
            if (!sceneAsset)
            {
                scenePath = string.Empty;
                sceneName = string.Empty;
            }

            if (!Application.isPlaying) EditorSceneManager.MarkAllScenesDirty();
        }
#endif
    }

}
