# Tool Kit

`com.dragon.toolkit` 是一个 Unity 通用工具包，提供日志、文件读写、进程执行、网络请求、断点续传、定时器、输入管理、事件信号、纹理处理、场景引用、Inspector 按钮以及编辑器扩展。

## Package 信息

| 项 | 值 |
| --- | --- |
| Package Name | `com.dragon.toolkit` |
| Version | `1.0.0` |
| Runtime Assembly | `ToolKit.Runtime` |
| Editor Assembly | `ToolKit.Editor` |
| Tests Assembly | `ToolKit.Tests`（仅 `UNITY_INCLUDE_TESTS` 时编译） |
| Runtime Namespace | `ToolKit` |
| Editor Namespace | `ToolKit.Editor` |

## 安装

通过 Unity Package Manager 选择 **Add package from disk...**，然后选择本目录下的 `package.json`。

也可以将此目录放入项目的 `Packages` 目录，并在项目 `Packages/manifest.json` 中引用本地路径。

## 运行时功能

### 日志系统

`DLogger` 是对 Unity Logger 的封装，支持：

- 普通、警告、错误和异常日志
- 自定义标签、颜色、时间戳和帧号
- 对象、集合和字典格式化
- 循环引用检测和输出深度限制
- 基于线程本地缓存的格式化缓冲区

首次访问前会自动初始化（`RuntimeInitializeOnLoadMethod`），需要精确控制时可显式调用 `DLogger.Init()`。

```csharp
DLogger.Log("Game", "初始化完成");
DLogger.LogWarning("Network", "连接超时");
DLogger.LogError("Save", "保存失败");
DLogger.LogException(exception);

// 输出控制
DLogger.ShowTimeInTag = true;
DLogger.ShowFrameInTag = true;
DLogger.MaxCollectionItems = 100;
DLogger.MaxDepth = 3;
```

### 文件与 JSON

`ToolFile` 提供：

- JSON 保存和读取
- 文本和二进制文件读写（写入采用 `.tmp` + `File.Replace`/`File.Move` 的原子替换，避免半截文件）
- 文件夹创建与查询
- 从磁盘加载 `Texture2D`
- StreamingAssets 协程读取

安装 `com.unity.nuget.newtonsoft-json` 后使用 Newtonsoft.Json，否则回退到 Unity `JsonUtility`。

```csharp
ToolFile.SaveJson(path, data);

PlayerData loaded = new PlayerData();
bool success = ToolFile.ReadJson(path, ref loaded);

string text = ToolFile.ReadText(path);
ToolFile.WriteBytes(path, bytes);
```

### 进程与字节工具

`Tool` 提供跨平台的外部进程调用与底层字节比较：

```csharp
// 自动选择 cmd.exe (Windows) / bash (Unix)，返回标准输出
string output = Tool.ExecuteCommand("git rev-parse HEAD", timeoutMs: 30000);

Tool.StartProcess(fileName, arguments);

// 非托管级别的字节比较
bool same = Tool.CompareBytes(bytesA, bytesB);

// 查找区间内缺失的数字
int[] missing = Tool.GetMissingNumbersInRange(data, 0, 100);
```

### 网络请求

`ToolNet` 提供基于 `UnityWebRequest` 的协程接口：

- HTTP GET
- JSON POST / 二进制 POST
- DELETE
- 字节文件下载
- 流式 POST 响应
- 本机 IPv4 地址查询
- `NetResult<T>` 统一结果结构

```csharp
StartCoroutine(ToolNet.Get(url, result =>
{
    if (result.Success)
        DLogger.Log(result.Data);
    else
        DLogger.LogError(result.Error);
}));

List<IPAddress> localAddresses = ToolNet.GetIPv4();
```

### 断点续传下载

网络下载模块包含：

- `HttpDownloader`：下载任务和回调管理，支持 `forceDownload` 强制重下
- `DownloadHandlerFileRange`：Range 请求、文件写入、进度和速度统计
- `StreamDownloadHandler`：分块响应处理

```csharp
var downloader = new HttpDownloader(fileUrl, "patch.zip");
downloader.StartCallback = () => DLogger.Log("Download", "开始下载");
downloader.FinishedCallback = success => DLogger.Log("Download", success);

StartCoroutine(downloader.DownloadPatchSupportResume());

float progress = downloader.GetProgress(); // 0~1
```

### 定时器

`TimerManager` 提供 Update 驱动的定时任务：

- `Delay`：延迟执行一次
- `Loop`：固定间隔重复执行（`count <= 0` 表示无限循环）
- `Sequence`：顺序执行一组回调（`loopCount` 控制整体循环次数）
- 暂停、恢复和取消
- `MainDispatcher`：将后台线程任务派发到 Unity 主线程

```csharp
TimerHandle delay = TimerManager.Delay(1f, () =>
{
    DLogger.Log("延迟一秒执行");
});

TimerHandle loop = TimerManager.Loop(
    interval: 0.5f,
    callback: () => DLogger.Log("循环执行"),
    count: 5
);

loop.Pause();
loop.Resume();
loop.Cancel();

// 子线程 → 主线程
Task.Run(() => TimerManager.MainDispatcher(() => DLogger.Log("回到主线程")));
```

### 输入管理

`InputManager` 同时支持旧 Input Manager 和新 Input System：

- 按键按下、抬起和持续触发
- Control、Shift、Alt 组合键
- 通过 Handle 移除绑定
- 查询当前修饰键状态

```csharp
InputBindingHandle binding = InputManager.AddKeyDown(
    InputKey.F,
    () => DLogger.Log("按下 F")
);

CombinationBindingHandle combination = InputManager.AddCombinationKey(
    ModifierKey.Control | ModifierKey.Shift,
    InputKey.F,
    () => DLogger.Log("Ctrl + Shift + F")
);

InputManager.RemoveKey(binding);
InputManager.RemoveCombinationKey(combination);

ModifierKey current = InputManager.GetCurrentModifiers();
InputManager.Clear();
```

### 信号系统

`Signals` 提供类型安全的全局事件系统，支持零到四个参数。也可以创建独立的 `SignalHub`。

```csharp
public sealed class ScoreChangedSignal : ASignal<int>
{
}

void OnEnable()
{
    Signals.Get<ScoreChangedSignal>().AddListener(OnScoreChanged);
}

void OnDisable()
{
    Signals.Get<ScoreChangedSignal>().RemoveListener(OnScoreChanged);
}

void UpdateScore(int score)
{
    Signals.Get<ScoreChangedSignal>().Dispatch(score);
}
```

### 单例

- `Singleton<T>`：普通 C# 类单例，可重写 `Init()`
- `SingletonMono<T>`：MonoBehaviour 单例，支持场景实例查找、自动创建和重复实例处理

```csharp
public sealed class GameService : Singleton<GameService>
{
}

public sealed class GameManager : SingletonMono<GameManager>
{
}
```

### 纹理工具

`ToolTexture` 提供屏幕截图、纹理裁剪和缩放、纹理区域合并以及 Compute Shader Alpha 合成。

屏幕截图接口仅在项目包含 `com.unity.modules.screencapture` 时编译。

```csharp
yield return ToolTexture.Capture(texture => screenshot = texture);

Texture2D resized = ToolTexture.ResizeTexture(source, 512, 512);
Texture2D cropped = ToolTexture.CropTexture(source, new Rect(0, 0, 256, 256));
ToolTexture.Release(temp);
```

### 数学、索引与内存

- `ToolMath`：宽高比缩放计算、按权重随机、归一化随机数组
- `IndexCounter`：`Loop` / `Clamp` 两种模式的索引移动，提供 `Next`/`Prev`/`TryNext`/`TryPrev`
- `ToolMemory`：结构体与非托管内存互转，大小端 `int` 读写
- `FPS`：当前、平均、最低和最高 FPS 统计

```csharp
var counter = new IndexCounter(length: 5, startIndex: 0, mode: IndexCounter.Mode.Loop);
int next = counter.Next();

if (counter.TryNext(out int newIndex))
{
    DLogger.Log(newIndex);
}
```

### PlayerPrefs

`ToolPrefs` 包装 PlayerPrefs，支持 `int`、`float`、`bool`、`string`、`Vector2`、`Vector3`、`Color`，每种类型都提供 `Get` / `GetOrCreate` / `Set` 三种形式。

```csharp
ToolPrefs.SetInt("Level", 3);
int level = ToolPrefs.GetInt("Level", defaultValue: 1);

Vector3 spawn = ToolPrefs.GetOrCreateVector3("SpawnPos", Vector3.zero);
```

### Unity 扩展方法

`UnityExtension` 提供：

- Vector2 与角度、弧度转换
- 集合随机元素、去重添加和洗牌
- Transform 复制、批量设置、重置和递归子节点查询

```csharp
transform.ResetTransform();
List<Transform> children = transform.GetAllChildren("Enemy");

Vector2 direction = 45f.DegToVector();
```

### 动画曲线

`CurveLooper` 支持以 `Loop`、`PingPong` 或 `Clamp` 模式计算 `AnimationCurve`。

```csharp
var looper = new CurveLooper(curve, 2f, CurveLoopMode.PingPong);
float value = looper.Evaluate(Time.time);
```

### 场景引用

`SceneSerialized` 在 Inspector 中保存 `SceneAsset`，在构建后使用场景路径和名称（基于 `ISerializationCallbackReceiver`，可直接替代裸 `SceneAsset` 引用）。

```csharp
[SerializeField]
private SceneSerialized targetScene;

void LoadScene()
{
    SceneManager.LoadScene(targetScene.ScenePath);
}
```

### 其他组件

- `SimpleCameraController`：编辑器风格的自由相机控制器
- `ToolGlobal`：包信息（`PackageName`、`Version`）与菜单路径常量（`ToolKitMenuName`、`AssetMenuName`、`GameObjectMenuName`）。新增编辑器菜单时请复用这些常量，而不是硬编码字符串：

```csharp
[MenuItem(ToolGlobal.ToolKitMenuName + "My Window", priority = -1000)]
private static void OpenMyWindow() { }
```

## Inspector 特性

### `[Button]` — 方法按钮

将方法显示为 Inspector 底部的按钮，支持颜色、分组、排序、启用模式、间距和尺寸。

基于 `CustomEditor` 实现，**与自定义 `CustomEditor` 冲突**。如果脚本已有 `CustomEditor`，请改用下面的 `[PropertyButton]`。

```csharp
[Button("保存数据", ButtonColor.Green, order: 1)]
private void SaveData() { }

// 带参数方法：参数可在 Inspector 中编辑
[Button("加载数据", ButtonColor.Blue, order: 2)]
private void LoadData(string path, int version) { }

// 同组按钮水平排列
[Button("重置位置", ButtonColor.Yellow, group: "变换操作")]
private void ResetPosition() { }

[Button("重置旋转", ButtonColor.Yellow, group: "变换操作")]
private void ResetRotation() { }

// 仅在播放模式启用
[Button("导出快照", ButtonColor.White, mode: ButtonMode.EnabledInPlayMode)]
private void ExportSnapshot() { }
```

| 参数 | 说明 |
| --- | --- |
| `name` | 按钮文本，留空使用方法名 |
| `color` | `ButtonColor.White` / `Green` / `Red` / `Yellow` / `Blue` |
| `group` | 分组名，同组按钮水平排列；留空不分组 |
| `order` | 显示顺序，数值越小越靠前 |
| `mode` | `AlwaysEnabled` / `EnabledInPlayMode` / `DisabledInPlayMode` |
| `spacing` | 按钮间距（像素，默认 5） |
| `buttonHeight` | 按钮高度（像素，默认 25） |
| `buttonWidth` | `-1` 占满整行（默认）、`0` 自适应文本、`>0` 固定像素 |

支持实例方法、静态方法，以及 `public` / `private` / `protected` 访问级别；参数类型支持 `int`、`float`、`string`、`bool`、Vector、Color、Enum、UnityObject 等。

### `[PropertyButton]` — 字段按钮

在**字段**上显示一个按钮，点击后调用指定方法。基于 `PropertyDrawer` 实现，**即使脚本有自定义 `CustomEditor` 也能正常工作**（包括 `DrawDefaultInspector()`）。

方法带不带参数由反射自动识别：带参数时会在按钮旁生成可编辑的参数输入字段。

```csharp
// 无参数方法：只绘制按钮
[PropertyButton(nameof(ResetHealth), "重置生命值", ButtonColor.Red)]
public int health = 100;

// 带参数方法：参数可在 Inspector 中编辑
[PropertyButton("AddValue", "添加", ButtonColor.Green, defaultParams: new object[] { 10 })]
public int score;

private void AddValue(int amount)
{
    score += amount;
}

// 按钮放在属性右侧并指定宽度
[PropertyButton("ClearName", "清空", ButtonColor.Red, ButtonPosition.Right, buttonWidth: 60)]
public string playerName = "Player";

// 只显示按钮，不显示字段本身
[PropertyButton("DoSomething", "执行操作", ButtonColor.Green, drawProperty: false)]
public bool dummyField;
```

| 参数 | 说明 |
| --- | --- |
| `methodName` | 要调用的方法名（必需，可用 `nameof`） |
| `buttonText` | 按钮文本，默认使用方法名 |
| `color` | 按钮颜色，同 `ButtonColor` |
| `position` | `ButtonPosition.Above`（默认）/ `Below` / `Left` / `Right` |
| `buttonHeight` | 按钮高度（默认 20） |
| `buttonWidth` | `Above`/`Below`：`-1` 占满整行（默认）、`0` 自适应、`>0` 固定像素；`Left`/`Right`：`-1` 默认 80px、`0` 自适应、`>0` 固定像素 |
| `drawProperty` | 是否绘制原属性字段，默认 `true`；设为 `false` 则只显示按钮 |
| `defaultParams` | 方法参数的初始值 |

关于 `defaultParams` 的注意事项：

- 只能是编译期常量（`null`、数值、`string`、`bool`、枚举值、`typeof(...)`），所以 `Color.red` / `Vector3.zero` 这类属性或静态字段写不进来
- 它是构造函数的最后一个参数，建议用命名参数传：`defaultParams: new object[] { 10 }`
- 长度不足的部分使用参数类型的默认值；类型不匹配时会尝试自动转换（例如 `int` 字面量到 `float` 参数），失败则回退到类型默认值并打印警告

另外，Unity 不会堆叠同一字段上的多个 `PropertyDrawer` 属性（只会有一个生效），所以一个字段只能挂一个 `[PropertyButton]`，需要多个按钮请拆成多个字段。

完整示例见 `Runtime/Examples/PropertyButtonExample.cs`。

### `[ReadOnly]`

标记序列化字段为只读（仅编辑器生效）。

```csharp
[ReadOnly]
[SerializeField]
private int runtimeValue;
```

### `[SearchableEnum]`

为枚举以及枚举数组提供可搜索的下拉选择。

```csharp
[SearchableEnum]
[SerializeField]
private KeyCode key;
```

### `[DebugData]` / `[DebugButton]`

这两个特性面向 **Debug Window**（见下节），不会在普通 Inspector 中显示。

```csharp
public class PlayerDebug : MonoBehaviour
{
    [DebugData("当前生命值")]
    private int health = 100;

    [DebugData]
    private float speed = 5f;

    [DebugButton("回满血", ButtonColor.Green, group: "状态", order: 0)]
    private void RestoreHealth() => health = 100;

    [DebugButton("扣血", ButtonColor.Red, group: "状态", order: 1)]
    private void TakeDamage(int amount) => health -= amount;
}
```

| 特性 | 目标 | 参数 |
| --- | --- | --- |
| `[DebugData]` | 字段 / 属性 | `name`（显示名称，留空用成员名） |
| `[DebugButton]` | 方法 | `name`、`color`、`group`、`order` |

## Debug Window

菜单 **Tool Kit/Debug Window** 打开集中式调试面板，把场景中所有带 `[DebugData]` / `[DebugButton]` 的组件汇总到一处，无需在 Hierarchy 里逐个找对象、展开 Inspector。

- 按组件分组折叠显示，标题右侧显示完整层级路径，点击可选中并 Ping 对应 GameObject
- 字段值可直接编辑，修改走 `Undo.RecordObject` + `EditorUtility.SetDirty`，可撤销且会正确标记场景脏
- 方法渲染为按钮，支持分组（同组水平排列）和 `order` 排序；带参数的方法会展开参数输入区，参数值跨刷新保留
- 组件扫描覆盖非激活对象，自动排除 Project 中的 Prefab / 资源
- 顶部搜索框可按物体名、组件名、成员名或层级路径过滤
- Hierarchy 发生变化时自动刷新，也可手动点击 **Refresh**

支持编辑/输入的类型：`int`、`float`、`double`、`long`、`bool`、`string`、`Vector2`、`Vector3`、`Vector4`、`Color`、枚举和 `UnityEngine.Object` 派生类型。

## SerializedDictionary

项目包含第三方可序列化字典实现（位于 `Runtime/Serialization/SerializedDictionary/`，命名空间 `ToolKit.SerializedDictionary`，编译进 `ToolKit.Runtime`）：

- 泛型 `SerializedDictionary<TKey, TValue>`
- 自定义 Key 和 Value 显示名称
- Inspector 增删、分页和搜索
- 重复 Key 检测与清理
- 数字、字符串和枚举搜索
- 枚举、整数范围和步进整数 Key 生成器

```csharp
using ToolKit.SerializedDictionary;

[SerializedDictionary("ID", "配置")]
[SerializeField]
private SerializedDictionary<int, ItemConfig> items = new();
```

## 编辑器功能

### 编辑器菜单

| 菜单项 | 说明 |
| --- | --- |
| `Tool Kit/创建新插件` | 打开创建 UPM Package 的窗口 |
| `Tool Kit/数值调试窗口` | 打开运行时数值调试窗口 |
| `Tool Kit/图标合集` | 浏览、搜索和导出 Unity 内置图标 |
| `Tool Kit/Debug Window` | 打开场景调试面板 |
| `Tool Kit/包配置切换` | 切换 Package 配置组合 |
| `Tool Kit/打开数据存储目录` | 打开 `Application.persistentDataPath` |
| `GameObject/Tool Kit/查找引用该对象的脚本` | 查找场景中引用当前选中对象的脚本和字段 |
| `Assets`、`GameObject/Tool Kit/删除缺少的脚本` | 递归移除 Missing Script |
| `Assets/卸载插件包` | 从 Assets 右键菜单卸载 Package |

### 自定义 Toolbar

`EditorToolbar` 可以向 Unity 顶部工具栏添加按钮、下拉框或自定义 UI Toolkit 元素。

```csharp
#if UNITY_EDITOR
using System.Collections.Generic;
using ToolKit.Editor;

EditorToolbar.AddButtonRight("刷新", Refresh);
EditorToolbar.AddButtonLift("重置", Reset);
EditorToolbar.AddDropdownField(
    new List<string> { "开发", "测试", "正式" },
    value => value,
    ToolbarLocation.Right,
    dropdown => DLogger.Log(dropdown.value)
);
EditorToolbar.AddElement<VisualElement>(ToolbarLocation.Left);
#endif
```

### 编辑器窗口与工具

- `CreatePackage`：创建新的 UPM Package
- `DebugValueWindow`：显示运行时调试值
- `DebugWindow`：场景调试面板（`[DebugData]` / `[DebugButton]`）
- `EditorIcons`：浏览、搜索和导出 Unity 内置图标
- `PackageProfileSwitcher`：切换 Package 配置组合
- `MissingScriptsRemover`：递归移除 Missing Script
- `PackageContextMenu`：从 Assets 右键菜单卸载 Package
- `LogStackJumpFix`：修复日志堆栈跳转行为

```csharp
DebugValueWindow.ShowValue("Player Position", transform.position);
DebugValueWindow.RemoveValue("Player Position");
```

### 按钮渲染工具

`ButtonRenderUtility`（`ToolKit.Editor`）是 `ButtonDrawer`、`PropertyButtonDrawer` 和 `DebugWindow` 共用的底层工具，提供：

- `ButtonColor` → `Color` 映射
- 带缓存的方法查找（支持同签名匹配）
- 带颜色的按钮绘制
- 方法调用与异常处理、返回值读取
- 参数默认值推导、类型自动转换、参数输入控件绘制与高度计算

## 条件编译

| Define | 来源 | 功能 |
| --- | --- | --- |
| `UNITY_ENGINE` | `com.dragon.toolkit` | 标识运行在 Unity 引擎环境 |
| `NEWTONSOFT_JSON` | `com.unity.nuget.newtonsoft-json` | 使用 Newtonsoft.Json |
| `SCREEN_CAPTURE` | `com.unity.modules.screencapture` | 启用屏幕截图接口 |
| `ENABLE_INPUT_SYSTEM` | Unity Player Settings | 使用新 Input System |
| `UNITY_INCLUDE_TESTS` | Unity Test Framework | 编译 `ToolKit.Tests` 程序集 |

## 目录结构

```text
ToolKit/
|-- Runtime/
|   |-- Animator/          CurveLooper
|   |-- Attributes/        Button / PropertyButton / DebugButton / DebugData / ReadOnly / SearchableEnum
|   |-- Core/              DLogger、ToolGlobal
|   |-- Examples/          PropertyButtonExample
|   |-- Helper/            SimpleCameraController
|   |-- Network/           ToolNet、NetResult
|   |   `-- Http/          HttpDownloader、DownloadHandlerFileRange、StreamDownloadHandler
|   |-- Serialization/     SceneSerialized
|   |   `-- SerializedDictionary/   第三方可序列化字典
|   |-- Test/              DLoggerTest
|   |-- Tools/             Tool、ToolFile、ToolMath、ToolMemory、ToolPrefs、ToolTexture
|   `-- Utility/           Signals、Singleton、SingletonMono、KeyManager、FPS、IndexCounter、UnityExtension
|       `-- Timer/         TimerManager、TimerHandle、UpdateTimer、SequenceTimer
|-- Editor/
|   |-- CustomToolbar/     EditorToolbar
|   |-- Drawers/           ButtonDrawer、PropertyButtonDrawer、ReadOnlyDrawer、SceneSerializedDrawer、SearchableEnumDrawer、SerializedDictionaryDrawer
|   |-- Utility/          ButtonRenderUtility、LogStackJumpFix、MissingScriptsRemover、PackageContextMenu
|   `-- Windows/           DebugValueWindow、EditorIcons、PackageProfileSwitcher
|       |-- CreateNewPackage/
|       `-- DebugUIWindow/ DebugWindow、DebugSceneScanner、DebugReflectionCache
|-- package.json
`-- README.md
```

## 测试状态

当前 `Runtime/Test/DLoggerTest.cs` 是手动运行的日志测试组件，尚未建立完整的 EditMode/PlayMode 自动测试和 CI 流程。
