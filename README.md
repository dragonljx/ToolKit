ToolKit Unity 包 — 功能概览
================================

简介
----
这是一个为 Unity3D 提供的一套实用工具包，包含运行时工具、编辑器扩展、序列化、网络与 NTP 支持，以及若干实用的辅助组件和示例资源。

主要功能模块（分组说明）
---------------------

- 通用运行时工具:
  - Tool.cs / ToolGlobal.cs：全局工具与启动入口
  - ToolFile.cs：文件读写与工具函数
  - ToolPrefs.cs：偏好设置封装
  - ToolMath.cs：数学工具
  - ToolMemory.cs：内存/缓存相关工具
  - ToolNet.cs：网络相关工具
  - ToolTexture.cs：纹理处理工具

- 实用工具集合（Utility）:
  - CustomPool.cs：对象池实现
  - FPS.cs：帧率监测
  - IndexCounter.cs：索引计数器
  - Json.cs：简单 JSON 支持/封装
  - KeyManager.cs：按键/输入管理
  - SerializedDictionary.cs：可序列化字典实现
  - Signals.cs：事件/信号系统
  - Singleton.cs / SingletonSO.cs：单例与 ScriptableObject 单例
  - TimerDispatcher.cs：定时分发器

- 动画相关（Animator）:
  - CurveLooper.cs：曲线循环器/曲线驱动辅助

- 属性与绘制器（Attributes / Drawers / Draw）:
  - ButtonAttribute.cs / ReadOnlyAttribute.cs：自定义属性
  - ReadOnlyDrawer.cs / SceneSerializedDrawer.cs：Inspector 绘制器
  - Draw/CustomizeGUI.cs：自定义 GUI 支持函数
  - Drawers/Button/*：多种按钮类与 Drawer 实现

- 编辑器扩展（Editor）:
  - 自定义工具栏：Editor/CustomToolbar/EditorToolbar.cs 与样式文件
  - 编辑器菜单与工具：EditorMenu.cs、EditorTool.cs
  - 缺失脚本清理器：MissingScriptsRemover.cs
  - 调试窗口与创建包窗口：Windows/ 下的 DebugValueWindow.cs 与 CreateNewPackage

- 编辑器资源与 UI:
  - Fonts、UXML/USS、图标等资源用于编辑器窗口与样式

- 网络（Network/Http）:
  - HttpDownloader.cs：HTTP 下载功能
  - DownloadHandlerFileRange.cs：支持范围/分块下载（可用于断点续传）

- NTP（时间同步）:
  - NtpClient.cs / NtpServer.cs：NTP 客户端与服务器实现
  - NtpClientPacket.cs / NtpServerPacket.cs：封包解析/构建

- 序列化与场景保存（Serialization）:
  - SceneSerialized.cs：场景/对象自定义序列化与恢复工具

- 核心/日志（Core）:
  - TLog.cs：日志与调试输出封装

- 扩展方法（Extension）:
  - GeneralExtension.cs、UnityExtension.cs、Serialization.cs：通用扩展方法集合

- 示例与资源:
  - ComputeShader 示例位于 Resources/ComputeShader

文件与类映射（示例）
-----------------

- 编辑器工具栏：Editor/CustomToolbar/EditorToolbar.cs
- 场景序列化：Runtime/Serialization/SceneSerialized.cs
- NTP 客户端：Runtime/NTP/NtpClient.cs
- HTTP 下载器：Runtime/Network/Http/HttpDownloader.cs
- 对象池：Runtime/Utility/CustomPool.cs
- 单例实现：Runtime/Utility/Singleton.cs

简要使用示例
-----------

1) 使用对象池（伪代码）：

   var pool = new CustomPool<MyType>();
   var obj = pool.Spawn();
   pool.Recycle(obj);

2) 使用 NTP 同步当前时间（伪代码）：

   var client = new NtpClient("time.server.com");
   client.RequestTime();
   var now = client.CurrentTime;

3) 使用 HttpDownloader 进行文件下载（伪代码）：

   var dl = new HttpDownloader(url, destPath);
   await dl.StartAsync();

下一步
-----
如果需要，我可以：

- 为每个模块生成更详细的类-方法清单（含所在文件与主要公有接口）。
- 提供若干可运行的使用示例脚本并放入 `Examples/` 目录。
- 为编辑器窗口生成更详细的使用说明（如何打开、如何自定义）。

请告诉我你想要的深度（例如：类列表、API 文档、示例脚本）。
