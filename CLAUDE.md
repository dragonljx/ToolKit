# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Package Info

- **Name**: `com.dragon.toolkit` — Unity utility toolkit package
- **Main assemblies**: `ToolKit.Runtime` (`ToolKit` namespace) and `ToolKit.Editor` (`ToolKit.Editor` namespace)
- **Third-party assemblies**: `ToolKit.ThirdParty.Runtime` / `ToolKit.ThirdParty.Editor` (SerializedDictionary), `ToolKit.Tests` (compiled only with `UNITY_INCLUDE_TESTS`)
- **Dependencies**: Newtonsoft.Json (via precompiled reference), com.unity.nuget.newtonsoft-json (define: `NEWTONSOFT_JSON`)
- **Define constraints**: `UNITY_ENGINE` (from version define), `SCREEN_CAPTURE` (from screencapture module)
- Both assemblies use `allowUnsafeCode: true`

## Architecture Overview

### Runtime (`Runtime/`) — `ToolKit` namespace

| Module | Path | Purpose |
|--------|------|---------|
| **Core** | `Runtime/Core/` | Core infrastructure: `DLogger` (logging with colored tags), `ToolGlobal` (menu path constants, package info) |
| **Tools** | `Runtime/Tools/` | Static utility classes: `Tool` (cross-platform process execution, byte comparison), `ToolFile` (file I/O, JSON, atomic writes), `ToolMath` (geometry, probability), `ToolMemory` (struct<->pointer, endianness), `ToolPrefs` (PlayerPrefs wrapper), `ToolTexture` (texture operations, screenshot, merge, crop) |
| **Network** | `Runtime/Network/` | `ToolNet` (HTTP GET/POST/DELETE, streaming), `Http/` (resumable downloader with range requests) |
| **Signals** | `Runtime/Utility/Signals.cs` | Type-safe event system (ASignal<T> up to 4 params). Use `Signals.Get<T>()` / `SignalHub` |
| **Singleton** | `Runtime/Utility/Singleton.cs` | MonoBehaviour singleton with auto-create and DontDestroyOnLoad |
| **SingletonSO** | `Runtime/Utility/SingletonSO.cs` | ScriptableObject singleton pattern |
| **CustomPool** | `Runtime/Utility/CustomPool.cs` | Object pool using Unity's ObjectPool. Four flavors: `CustomPool`, `CustomPool<T>`, `CustomPoolSingleton<T>`, `CustomPoolSingleton<T,T1>`, and non-MonoBehaviour `GameobjectPool` |
| **TimerDispatcher** | `Runtime/Utility/TimerDispatcher.cs` | Timer system — Update-based timers, async timers (Threading.Timer), coroutine helpers, and sequential queue timers |
| **SerializedDictionary** | `Runtime/ThirdParty/SerializedDictionary/` | Third-party serializable dictionary (isolated assembly `ToolKit.ThirdParty.Runtime`) |
| **KeyManager** | `Runtime/Utility/KeyManager.cs` | Input key management |
| **Animator/CurveLooper** | `Runtime/Animator/CurveLooper.cs` | Animation curve looping/driving |
| **NTP** | `Runtime/NTP/` | NTP client/server for network time sync (TODO: verify existence) |
| **Network/Http** | `Runtime/Network/Http/` | HTTP downloader with range-request resume support (`HttpDownloader`, `DownloadHandlerFileRange`, `StreamDownloadHandler`) |
| **Serialization** | `Runtime/Serialization/SceneSerialized.cs` | Scene reference serialization — ISerializationCallbackReceiver-based, safe in builds |
| **Attributes** | `Runtime/Attributes/` | `ButtonAttribute` (method buttons), `PropertyButtonAttribute` (field-based buttons, with automatic parameter input, works with CustomEditor), `ReadOnlyAttribute`, `DebugButtonAttribute`, `DebugDataAttribute` |
| **Extension** | `Runtime/Extension/` | `GeneralExtension`, `UnityExtension` (Transform/collection/math extensions), `Serialization` |

### Editor (`Editor/`) — `ToolKit.Editor` namespace

| Module | Path | Purpose |
|--------|------|---------|
| **EditorMenu** | `Editor/EditorMenu.cs` | Menu items under "Tool Kit/" — CreatePackage, DebugValueWindow, EditorIcons, open data path, remove unused packages |
| **EditorTool** | `Editor/EditorTool.cs` | Additional editor utility methods |
| **CustomToolbar** | `Editor/CustomToolbar/EditorToolbar.cs` | Extends the Unity toolbar with custom UI elements (buttons, dropdowns) using UI Toolkit. Static `AddButton`/`AddDropdownField` API |
| **MissingScriptsRemover** | `Editor/MissingScriptsRemover.cs` | Cleans missing script references from GameObjects |
| **PackageContextMenu** | `Editor/PackageContextMenu.cs` | Context menu extensions for the Package Manager |
| **Windows** | `Editor/Windows/` | `DebugValueWindow`, `EditorIcons`, `PackageProfileSwitcher` (apply named package profiles via PackageManager.Client), `CreateNewPackage` |
| **Drawers** | `Editor/Drawers/` | Property drawers: `ButtonDrawer` (method-based buttons via CustomEditor), `PropertyButtonDrawer` (field-based buttons, CustomEditor-compatible, handles parameter input when the target method has parameters), `ReadOnlyDrawer`, `SceneSerializedDrawer`, `SearchableEnumDrawer` |

### Key Patterns

- **Menu registration**: Use `ToolGlobal.*MenuName` constants for menu item paths. Priority below -1000 places items high in the menu.
- **ButtonAttribute vs PropertyButton**:
  - `ButtonAttribute`: Method-based, use for normal scripts without CustomEditor. Feature-rich (grouping, ordering, async support).
  - `PropertyButtonAttribute`: Field-based, use when script has CustomEditor. Works with DrawDefaultInspector(). Supports positions (Above/Below/Left/Right), colors, and hiding the property. Parameter input is generated automatically when the target method has parameters — there is no separate "WithParams" attribute. Pass initial parameter values as a named argument: `[PropertyButton("Add", defaultParams: new object[] { 10 })]`. Only compile-time constants are valid attribute arguments (no `Color.red` / `Vector3.zero`). Unity does not stack multiple PropertyDrawer attributes on one field, so one field = one button.
- **SceneSerialized**: Use this type instead of bare SceneAsset references. It serializes the scene path for builds while showing a nice SceneAsset picker in the Editor.
- **DLogger**: Use `DLogger.Log`/`LogWarning`/`LogError` instead of `Debug.Log` — adds colored tag prefixes and deep object inspection.
- **Signals**: Define custom signal classes extending `ASignal<T>` and dispatch via `Signals.Get<T>()`.
- **EditorToolbar**: Call `EditorToolbar.AddButton()`/`AddDropdownField()` from any static init — no hook into the toolbar rendering needed.

### Comment style

All code comments follow the XML doc structure established by `Runtime/Attributes/PropertyButtonAttribute.cs`. Write comments in Chinese. This is the reference implementation — read it before adding docs to a new file.

```csharp
/// <summary>
/// 一句话说明这个类型做什么（直接描述，不加类型名前缀）
/// <para>用法：在哪里、怎么用</para>
/// <para>自动推断或隐含的行为（例如"参数由反射自动识别"）</para>
/// <code>
/// // 用例 1：最小可用写法
/// // 用例 2：进阶写法，含被调用方的完整方法体，可整段复制运行
/// </code>
/// <para>与相关 API 的差异、选择指引（"与 X 不同，…"）</para>
/// </summary>
/// <remarks>
/// <para>进阶注意事项的主题句：</para>
/// <list type="bullet">
/// <item>条目一，技术标识符用 <c>标识符</c> 包裹</item>
/// </list>
/// <para>补充说明，每个 para 独占一行，用来替代 br</para>
/// </remarks>
[AttributeUsage(...)]
public sealed class XxxAttribute : Yyy
```

- **Types** (class / struct / enum / interface): `summary` states the one-line purpose, then `para` lines for usage and key behavior, a `code` block with runnable examples, and a closing `para` comparing it against related APIs. `remarks` holds the advanced caveats, using a `para` topic sentence plus a `list type="bullet"`.
- **Members** (properties / fields): one-line `summary`, plus an optional `para` for default value, accepted range, or per-scenario differences.
- **Constructors / methods**: one `<param>` per parameter, each on its own line, stating "必需" / "默认 XX" / accepted values.
- **Enum members**: one-line `summary`.

`code` blocks must be compilable — include the callee's full signature and body so the example can be copied wholesale. Skip comments that carry no information (`/// <summary>获取值</summary>`).

## Development

This is a Unity package (shipped via embedded/computed path). There are no test runners, build scripts, or CI configuration in the repo. Edit `.cs` files; changes take effect on Unity recompile. The `.uss` file for toolbar styling is at `Editor/CustomToolbar/ToolbarStyle.uss`.

### Common tasks

- **Add a new utility to Runtime**: Create the file in the appropriate subdirectory under `Runtime/`, use `namespace ToolKit`.
- **Add a new editor window**: Create in `Editor/Windows/`, register via `[MenuItem(ToolGlobal.ToolKitMenuName + "Window Name")]`.
- **Add a custom property drawer**: Create drawer class in `Editor/Drawers/`.
- **Add a new signal type**: Create a class extending `ASignal<T>` (or parameterless variants), use `Signals.Get<YourSignal>()`.
