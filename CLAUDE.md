# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Package Info

- **Name**: `com.dragon.toolkits` — Unity utility toolkit package
- **Two assemblies**: `Toolkits.Runtime` (`ToolKits` namespace) and `ToolKits.Editor` (`ToolKitsEditor` namespace)
- **Dependencies**: Newtonsoft.Json (via precompiled reference), com.unity.nuget.newtonsoft-json (define: `NEWTONSOFT_JSON`)
- **Define constraints**: `UNITY_ENGINE` (from version define), `SCREEN_CAPTURE` (from screencapture module)
- Both assemblies use `allowUnsafeCode: true`

## Architecture Overview

### Runtime (`Runtime/`) — `ToolKits` namespace

| Module | Path | Purpose |
|--------|------|---------|
| **Core/TLog** | `Runtime/Core/TLog.cs` | Logging wrapper around UnityLogger with colored tags and object inspection |
| **Tool* helpers** | `Runtime/Tool*.cs` | Static utility classes: `Tool` (process execution, byte comparison), `ToolFile`, `ToolMath`, `ToolMemory`, `ToolNet`, `ToolPrefs`, `ToolTexture` |
| **ToolGlobal** | `Runtime/ToolGlobal.cs` | Menu path constants (`Tool Kit/`, `Assets/Tool Kit/`, `GameObject/Tool Kit/`) |
| **Signals** | `Runtime/Utility/Signals.cs` | Type-safe event system (ASignal<T> up to 4 params). Use `Signals.Get<T>()` / `SignalHub` |
| **Singleton** | `Runtime/Utility/Singleton.cs` | MonoBehaviour singleton with auto-create and DontDestroyOnLoad |
| **SingletonSO** | `Runtime/Utility/SingletonSO.cs` | ScriptableObject singleton pattern |
| **CustomPool** | `Runtime/Utility/CustomPool.cs` | Object pool using Unity's ObjectPool. Four flavors: `CustomPool`, `CustomPool<T>`, `CustomPoolSingleton<T>`, `CustomPoolSingleton<T,T1>`, and non-MonoBehaviour `GameobjectPool` |
| **TimerDispatcher** | `Runtime/Utility/TimerDispatcher.cs` | Timer system — Update-based timers, async timers (Threading.Timer), coroutine helpers, and sequential queue timers |
| **SerializedDictionary** | `Runtime/Utility/SerializedDictionary.cs` | ISerializationCallbackReceiver-based serializable dictionary |
| **KeyManager** | `Runtime/Utility/KeyManager.cs` | Input key management |
| **Animator/CurveLooper** | `Runtime/Animator/CurveLooper.cs` | Animation curve looping/driving |
| **NTP** | `Runtime/NTP/` | NTP client/server for network time sync |
| **Network/Http** | `Runtime/Network/Http/` | HTTP downloader with range-request resume support (`HttpDownloader`, `DownloadHandlerFileRange`, `StreamDownloadHandler`) |
| **Serialization** | `Runtime/Serialization/SceneSerialized.cs` | Scene reference serialization — ISerializationCallbackReceiver-based, safe in builds |
| **Attributes** | `Runtime/Attributes/` | `ButtonAttribute` (inspector method buttons), `ReadOnlyAttribute` |
| **Extension** | `Runtime/Extension/` | `GeneralExtension`, `UnityExtension` (Transform/collection/math extensions), `Serialization` |

### Editor (`Editor/`) — `ToolKitsEditor` namespace

| Module | Path | Purpose |
|--------|------|---------|
| **EditorMenu** | `Editor/EditorMenu.cs` | Menu items under "Tool Kit/" — CreatePackage, DebugValueWindow, EditorIcons, open data path, remove unused packages |
| **EditorTool** | `Editor/EditorTool.cs` | Additional editor utility methods |
| **CustomToolbar** | `Editor/CustomToolbar/EditorToolbar.cs` | Extends the Unity toolbar with custom UI elements (buttons, dropdowns) using UI Toolkit. Static `AddButton`/`AddDropdownField` API |
| **MissingScriptsRemover** | `Editor/MissingScriptsRemover.cs` | Cleans missing script references from GameObjects |
| **PackageContextMenu** | `Editor/PackageContextMenu.cs` | Context menu extensions for the Package Manager |
| **Windows** | `Editor/Windows/` | `DebugValueWindow`, `EditorIcons`, `PackageProfileSwitcher` (apply named package profiles via PackageManager.Client), `CreateNewPackage` |
| **Drawers** | `Editor/Drawers/` | Property drawers: `ReadOnlyDrawer`, `SceneSerializedDrawer`, and a full Button drawer system (supports params, async, mode-based enable/disable) |

### Key Patterns

- **Menu registration**: Use `ToolGlobal.*MenuName` constants for menu item paths. Priority below -1000 places items high in the menu.
- **ButtonAttribute**: `[Button]` on methods to show clickable buttons in the Inspector. Supports `ButtonMode` (AlwaysEnabled/EnabledInPlayMode/DisabledInPlayMode), `ButtonSpacing`, and parameterized methods with foldout state (`Expanded`).
- **SceneSerialized**: Use this type instead of bare SceneAsset references. It serializes the scene path for builds while showing a nice SceneAsset picker in the Editor.
- **TLog**: Use `TLog.Log`/`LogWarning`/`LogError` instead of `Debug.Log` — adds colored tag prefixes and deep object inspection.
- **Signals**: Define custom signal classes extending `ASignal<T>` and dispatch via `Signals.Get<T>()`.
- **EditorToolbar**: Call `EditorToolbar.AddButton()`/`AddDropdownField()` from any static init — no hook into the toolbar rendering needed.

## Development

This is a Unity package (shipped via embedded/computed path). There are no test runners, build scripts, or CI configuration in the repo. Edit `.cs` files; changes take effect on Unity recompile. The `.uss` file for toolbar styling is at `Editor/CustomToolbar/ToolbarStyle.uss`.

### Common tasks

- **Add a new utility to Runtime**: Create the file in the appropriate subdirectory under `Runtime/`, use `namespace ToolKits`.
- **Add a new editor window**: Create in `Editor/Windows/`, register via `[MenuItem(ToolGlobal.ToolKitMenuName + "Window Name")]`.
- **Add a custom property drawer**: Create drawer class in `Editor/Drawers/`.
- **Add a new signal type**: Create a class extending `ASignal<T>` (or parameterless variants), use `Signals.Get<YourSignal>()`.
