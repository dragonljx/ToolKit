# Tool Kit

`com.dragon.toolkit` 是一个 Unity 通用工具包，提供日志、文件读写、网络请求、断点续传、定时器、输入管理、事件信号、纹理处理、场景引用以及编辑器扩展。

## Package 信息

- Package Name：`com.dragon.toolkit`
- Runtime Assembly：`ToolKit.Runtime`
- Editor Assembly：`ToolKit.Editor`
- Runtime Namespace：`ToolKit`
- Editor Namespace：`ToolKit.Editor`

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

```csharp
DLogger.Log("Game", "初始化完成");
DLogger.LogWarning("Network", "连接超时");
DLogger.LogError("Save", "保存失败");
DLogger.LogException(exception);
```

### 文件与 JSON

`ToolFile` 提供：

- JSON 保存和读取
- 文本和二进制文件读写
- 文件夹创建
- 从磁盘加载 `Texture2D`
- StreamingAssets 协程读取

安装 `com.unity.nuget.newtonsoft-json` 后使用 Newtonsoft.Json，否则回退到 Unity `JsonUtility`。

```csharp
ToolFile.SaveJson(path, data);

PlayerData loaded = new PlayerData();
bool success = ToolFile.ReadJson(path, ref loaded);
```

### 网络请求

`ToolNet` 提供基于 `UnityWebRequest` 的协程接口：

- HTTP GET
- JSON POST
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
```

### 断点续传下载

网络下载模块包含：

- `HttpDownloader`：下载任务和回调管理
- `DownloadHandlerFileRange`：Range 请求、文件写入、进度和速度统计
- `StreamDownloadHandler`：分块响应处理

```csharp
var downloader = new HttpDownloader(fileUrl, "patch.zip");
downloader.StartCallback = () => DLogger.Log("Download", "开始下载");
downloader.FinishedCallback = success => DLogger.Log("Download", success);

StartCoroutine(downloader.DownloadPatchSupportResume());
```

### 定时器

`TimerManager` 提供 Update 驱动的定时任务：

- `Delay`：延迟执行一次
- `Loop`：固定间隔重复执行
- `Sequence`：顺序执行一组回调
- 暂停、恢复和取消
- 将后台线程任务派发到 Unity 主线程

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

- `Singleton<T>`：普通 C# 类单例
- `SingletonMono<T>`：MonoBehaviour 单例，支持场景实例查找、自动创建和重复实例处理

```csharp
public sealed class GameService : Singleton<GameService>
{
    private GameService()
    {
    }
}

public sealed class GameManager : SingletonMono<GameManager>
{
}
```

### 纹理工具

`ToolTexture` 提供屏幕截图、纹理裁剪和缩放、纹理区域合并以及 Compute Shader Alpha 合成。

屏幕截图接口仅在项目包含 `com.unity.modules.screencapture` 时编译。

### 数学、索引与内存

- `ToolMath`：宽高比计算、权重随机、归一化随机数组
- `IndexCounter`：循环或 Clamp 模式的索引移动
- `ToolMemory`：结构体与非托管内存转换、整数写入字节数组
- `FPS`：当前、平均、最低和最高 FPS 统计

### Unity 扩展方法

`UnityExtension` 提供：

- Vector2 与角度、弧度转换
- 集合随机元素、去重添加和洗牌
- 集合调试输出
- Transform 复制、重置和递归子节点查询

### 动画曲线

`CurveLooper` 支持以 `Loop`、`PingPong` 或 `Clamp` 模式计算 `AnimationCurve`。

```csharp
var looper = new CurveLooper(curve, 2f, CurveLoopMode.PingPong);
float value = looper.Evaluate(Time.time);
```

### 场景引用

`SceneSerialized` 在 Inspector 中保存 `SceneAsset`，在运行时使用场景路径和名称。

```csharp
[SerializeField]
private SceneSerialized targetScene;

void LoadScene()
{
    UnityEngine.SceneManagement.SceneManager.LoadScene(targetScene.ScenePath);
}
```

### Inspector Attributes

- `[Button]`：将方法显示为 Inspector 按钮，支持颜色、分组、排序和参数
- `[ReadOnly]`：只读字段
- `[SearchableEnum]`：支持搜索的枚举及枚举数组

```csharp
[Button("重新生成", ButtonColor.Green, group: "工具", order: 0)]
private void Rebuild()
{
}

[ReadOnly]
[SerializeField]
private int runtimeValue;

[SearchableEnum]
[SerializeField]
private KeyCode key;
```

### 其他组件

- `SimpleCameraController`：编辑器风格的自由相机控制器
- `ToolPrefs`：PlayerPrefs 类型扩展，支持 `bool`、`Vector2` 和 `Vector3`
- `ToolGlobal`：统一的编辑器菜单路径常量

## SerializedDictionary

项目包含第三方可序列化字典实现：

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

菜单入口位于 `Tool Kit/`：

- 创建新插件
- 数值调试窗口
- 图标合集
- 包配置切换
- 打开数据存储目录
- 卸载不常用插件

### 自定义 Toolbar

`EditorToolbar` 可以向 Unity 顶部工具栏添加按钮、下拉框或自定义 UI Toolkit 元素。

```csharp
#if UNITY_EDITOR
using System.Collections.Generic;
using ToolKit.Editor;

EditorToolbar.AddButtonRight("刷新", Refresh);
EditorToolbar.AddDropdownField(
    new List<string> { "开发", "测试", "正式" },
    value => value,
    ToolbarLocation.Right,
    dropdown => DLogger.Log(dropdown.value)
);
#endif
```

### 编辑器窗口与工具

- `CreatePackage`：创建新的 UPM Package
- `DebugValueWindow`：显示运行时调试值
- `EditorIcons`：浏览、搜索和导出 Unity 内置图标
- `PackageProfileSwitcher`：切换 Package 配置组合
- `MissingScriptsRemover`：递归移除 Missing Script
- `PackageContextMenu`：从 Assets 右键菜单卸载 Package
- `LogStackJumpFix`：修复日志堆栈跳转行为

```csharp
DebugValueWindow.ShowValue("Player Position", transform.position);
DebugValueWindow.RemoveValue("Player Position");
```

## 条件编译

| Define | 来源 | 功能 |
| --- | --- | --- |
| `NEWTONSOFT_JSON` | `com.unity.nuget.newtonsoft-json` | 使用 Newtonsoft.Json |
| `SCREEN_CAPTURE` | `com.unity.modules.screencapture` | 启用屏幕截图接口 |
| `ENABLE_INPUT_SYSTEM` | Unity Player Settings | 使用新 Input System |
| `UNITY_INCLUDE_TESTS` | Unity Test Framework | 编译测试程序集 |

## 目录结构

```text
ToolKit/
|-- Runtime/
|   |-- Animator/
|   |-- Attributes/
|   |-- Core/
|   |-- Helper/
|   |-- Network/
|   |-- Serialization/
|   |-- ThirdParty/
|   `-- Utility/
|-- Editor/
|   |-- CustomToolbar/
|   |-- Drawers/
|   |-- ThirdParty/
|   |-- Utility/
|   `-- Windows/
|-- package.json
`-- README.md
```

## 测试状态

当前 `Runtime/Test/DLoggerTest.cs` 是手动运行的日志测试组件，尚未建立完整的 EditMode/PlayMode 自动测试和 CI 流程。
