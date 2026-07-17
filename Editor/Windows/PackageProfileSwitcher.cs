using System.Collections.Generic;
using ToolKit;
using UnityEditor;
using UnityEditor.PackageManager;
using UnityEngine;

namespace ToolKitsEditor
{
    public class PackageProfileSwitcher : EditorWindow
    {
        private static readonly Dictionary<string, PackageProfile> Profiles = new()
        {
            {
                "Minimal",
                new PackageProfile
                {
                    Add = new[]
                    {
                        "com.unity.ugui",
                        "com.unity.textmeshpro",
                        "com.unity.modules.audio",
                        "com.unity.modules.ui",
                        "dragon.toolkits",
                        "com.unity.ide.rider"
                    },
                    Remove = new[]
                    {
                        "com.unity.collab-proxy",
                        "com.unity.feature.development",
                        "com.unity.visualscripting",
                        "com.unity.timeline"
                    }
                }
            },
            {
                "Timeline_Project",
                new PackageProfile
                {
                    Add = new[]
                    {
                        "com.unity.timeline",
                        "com.unity.modules.director"
                    },
                    Remove = new[]
                    {
                        "com.unity.visualscripting"
                    }
                }
            },
            {
                "Dev_Clean",
                new PackageProfile
                {
                    Add = new string[0],
                    Remove = new[]
                    {
                        "com.unity.collab-proxy",
                        "com.unity.feature.development"
                    }
                }
            }
        };
        [MenuItem(ToolGlobal.ToolKitMenuName + "包配置切换")]
        public static void Open()
        {
            GetWindow<PackageProfileSwitcher>("Package Switcher");
        }

        private void OnGUI()
        {
            GUILayout.Label("Package Profiles", EditorStyles.boldLabel);

            foreach (var profile in Profiles)
            {
                if (GUILayout.Button($"Apply: {profile.Key}", GUILayout.Height(30)))
                {
                    ApplyProfile(profile.Key);
                }
            }
        }

        private static void ApplyProfile(string profileName)
        {
            if (!Profiles.TryGetValue(profileName, out var profile))
            {
                Debug.LogError($"Profile not found: {profileName}");
                return;
            }

            Debug.Log($"[PackageSwitcher] Applying profile: {profileName}");

            foreach (var pkg in profile.Remove)
            {
                Debug.Log($"Removing: {pkg}");
                Client.Remove(pkg);
            }

            foreach (var pkg in profile.Add)
            {
                Debug.Log($"Adding: {pkg}");
                Client.Add(pkg);
            }
        }

        private class PackageProfile
        {
            public string[] Add;
            public string[] Remove;
        }
    }
}