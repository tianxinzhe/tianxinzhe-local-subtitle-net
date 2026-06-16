# Lemon Subtitle Studio - 功能实现情况分析报告

## 概述

本报告对照 `specfornet.md` 技术规格文档，对当前 `d:\Project_AI\local-subtitle-net` 仓库中的实际代码进行逐项核查，输出实现状态与差距清单。

> **总体结论**：项目目前处于 **UI 框架 + 脚手架** 阶段。导航、6 个页面、样式、设置、模型下载、日志、文件选择等"外壳"已就绪，但所有 **核心业务逻辑（音频提取、语音识别、字幕翻译、播放控制、字幕文件 I/O、数据库）均为占位实现** 或 **完全缺失**。整体可运行框架完成度约 **30%**，业务可用度约 **5%**。

---

## 一、当前代码结构（已存在的实物）

```
LemonSubtitleStudio.sln                       (单项目方案，不含 Core / Infrastructure / Tests)
LemonSubtitleStudio/                          (唯一的 WPF 项目)
├── App.xaml / App.xaml.cs                    (Prism 启动 + DI 注册)
├── Views/   (7 个 XAML, 6 业务页 + Shell)
├── ViewModels/ (6 个, 与页面一一对应)
├── Services/  (5 接口 + 5 实现)
│   ├── IFileService / FileService            ✅ 文件对话框
│   ├── ISettingsService / SettingsService    ✅ XML 配置存取
│   ├── IModelManagerService / ModelManagerService ✅ Whisper 模型下载
│   ├── ITaskQueueService / TaskQueueService  ⚠️ 仅含伪进度
│   └── ILoggingService / LoggingService      ✅ 日志事件 + 文件
├── Models/ (TaskItem, SubtitleItem)          ✅ 简化版领域模型
├── Converters/ (TaskStatusConverter, StatusColorConverter) ✅
└── Resources/Styles.xaml                     ✅
```

依赖（已在 `.csproj` 中）：Prism.Wpf 8.1.97, Whisper.net 1.5.0, Whisper.net.Runtime, Microsoft.ML.OnnxRuntime.DirectML 1.16.0, Xabe.FFmpeg 5.3.1, System.Data.SQLite.Core 1.0.118

---

## 二、与 Spec 的逐项对照

### 2.1 项目结构

| Spec 要求 | 现状 | 状态 |
|---|---|---|
| 4 个项目：`LemonSubtitleStudio` + `LemonSubtitleStudio.Core` + `LemonSubtitleStudio.Infrastructure` + `LemonSubtitleStudio.Tests` | 仅有 1 个 WPF 项目，sln 包含 1 个 csproj | ❌ 不符合规格 |
| Prism + 模块化（`MainModule.cs`） | 使用 Prism，但 `ConfigureModuleCatalog` 为空 | ⚠️ 半完成 |
| 严格三层：UI / Services / Infrastructure | 全部 Services 平铺在 UI 项目内 | ❌ |

### 2.2 领域模型

| Spec 类型 | 当前类型 | 差距 |
|---|---|---|
| `Task` (Id, Filename, FileSize, OriginalPath, Status, Progress, Eta, Error, OutputPaths) | `TaskItem` (Id, InputPath, OutputPath, FileName, Status, Progress, ErrorMessage, MediaPath) | 缺少 `FileSize / Eta / OutputPaths`；无独立 `TaskStatus` 与 `OutputPaths` 类 |
| `SubtitleRow` (字符串时间) | `SubtitleItem` (TimeSpan 时间) | **时间字段类型不一致**；缺少 `IsSelected` 的 setter 通知 |
| `SystemInfo` (GpuAvailable, GpuName, CpuCores, ThreadPoolSize) | 不存在 | ❌ |
| `HistoryRecord` | 不存在 | ❌ |
| `GlobalConfig` | 被 `SettingsData` 替代，且字段集与 spec 不同 | ⚠️ |
| `ModelSetting` (EFCore 实体) | 不存在 | ❌ |
| `TaskStatus` (Pending/Extracting/Transcribing/Translating/Completed/Error) | `TaskStatus` (Waiting/Processing/Completed/Failed) | 枚举值不匹配 |

### 2.3 服务接口

| Spec 接口 | 当前实现 | 状态 |
|---|---|---|
| `IAudioService`（ExtractAudio / ConvertToWav / IsFfmpegAvailable） | **不存在**，未引用 Xabe.FFmpeg | ❌ |
| `ITranscriptionService`（Transcribe / IsModelAvailable / GetAvailableModels） | **不存在**，未引用 Whisper.net | ❌ |
| `ITranslationService` | **不存在**，未封装 ONNX 翻译 | ❌ |
| `IModelManagerService` | 已实现但接口与 spec 不同（spec 是下载+默认模型管理，当前是简化版） | ⚠️ |
| `IHistoryService` | **不存在** | ❌ |
| `ISystemService` | **不存在** | ❌ |

### 2.4 数据库

| Spec 要求 | 现状 |
|---|---|
| EF Core + SQLite `AppDbContext` | ❌ |
| `history_records` 表 | ❌ |
| `model_settings` 表 | ❌ |
| 持久化在 `%APPDATA%\LemonSubtitleStudio\app.db` | 当前 Settings 用 `settings.xml` 写入 CWD |

### 2.5 UI 页面（按 spec 7.3.x 检查）

#### 视频转字幕 (VideoToSubtitleView)
| Spec 元素 | 实现 |
|---|---|
| 标题栏：文件名计数 + 内存/CPU + 导出全部/开始处理 | ✅ 有 |
| 配置栏：语言/精度/模型/输出目录 | ✅ |
| 文件队列（拖拽 + 可拖拽调整宽度 200-400px） | ✅ GridSplitter 已配置 200/400 |
| 视频预览面板（折叠） | ⚠️ 改为 `TabControl` 中的 `MediaElement`，**无 Source 绑定**，**未实现"完成后双击预览"** |
| 字幕列表面板（带"编辑"按钮跳转） | ⚠️ 编辑按钮只 Log，**未跳转到 `SubtitleEditorView` 并传参** |
| 任务进度多阶段（提取/识别/翻译） | ❌ 只有单一进度条 |
| 处理日志 | ✅ |
| **重试机制** | ❌ |
| 拖放文件 | ✅ |

#### 视频转音频 (VideoToAudioView)
| Spec 元素 | 实现 |
|---|---|
| 配置栏：WAV/MP3 切换 + 比特率 192-320 kbps | ✅ 已有 ComboBox + Slider |
| 文件队列 | ✅ |
| 音频波形预览（折叠） | ⚠️ Tab 内是占位 `TextBlock` |
| 播放控制（折叠） | ⚠️ 按钮无 `Command` 绑定 |
| 任务进度 / 日志 | ✅ |
| **真实音频提取** | ❌ `StartProcessing` 是 `Task.Delay` 伪进度 |

#### 音频转字幕 (AudioToSubtitleView)
| Spec 元素 | 实现 |
|---|---|
| 配置栏：语言/模型/输出目录 | ✅ |
| 文件队列 | ✅ |
| 字幕列表 + 编辑按钮跳转 | ⚠️ 编辑按钮只 Log，**未跳转** |
| 不需要波形预览 | ✅ 已省略 |
| **真实 Whisper 识别** | ❌ 伪进度 |

#### 字幕翻译 (SubtitleTranslationView)
| Spec 元素 | 实现 |
|---|---|
| 配置栏：源/目标语言 + 输出目录 | ✅ |
| 文件队列 | ✅ |
| 源字幕 / 目标字幕双面板 | ✅ |
| **真实 ONNX 翻译** | ❌ 伪进度 |
| **双语字幕导出** | ❌ |

#### 字幕编辑 (SubtitleEditorView)
| Spec 元素 | 实现 |
|---|---|
| 预览区域（视频/波形自动切换） | ❌ 只有 `MediaElement`，**无 Source 绑定 / 播放控制命令** |
| 字幕时间轴（可拖拽） | ❌ |
| 原文/译文编辑区 | ⚠️ 有原文编辑框，**无译文独立编辑** |
| 字幕列表（双击编辑、点击跳转） | ⚠️ DataGrid 无双击行为 |
| **外部跳转接收 (从其他页面带路径进来)** | ❌ |

#### 软件设置 (SettingsView)
| Spec 元素 | 实现 |
|---|---|
| 模型 Tab（语音识别 / 翻译） | ❌ 只有单一"模型设置"区域，**无 Tab 切换** |
| 模型列表 [下载/设为默认/删除] | ❌ 没有列出模型清单 + 操作按钮 |
| 模型存储路径 + 浏览 | ✅ |
| 默认输出目录 + 浏览 | ✅ |
| 字幕命名规则（保持原名/按语言/自定义） | ❌ 缺失 |
| 恢复默认设置 | ❌ 缺失 |
| 持久化到 AppData | ⚠️ 当前存到 `settings.xml`（CWD） |

### 2.6 导航与外壳

| Spec 元素 | 实现 |
|---|---|
| 侧边栏 6 项导航 | ✅ |
| Logo + GPU/CPU 徽章 | ❌ Logo 有，**无 GPU 徽章**（spec 7.2 要求"右上角浮动"） |
| 语言选择器（底部） | ✅ |
| 版本/线程信息 | ⚠️ 只有版本号，无线程数 |
| 内存/CPU 实时监控（1 秒） | ❌ `MemoryUsage` / `CpuUsage` 属性是静态占位，**无后台更新** |
| 性能监控自动滚动日志 | ❌ |

### 2.7 交互规范（spec 7.4）

| 项 | 状态 |
|---|---|
| 文件拖拽高亮 | ❌ |
| 任务状态视觉表现（图标 + 颜色） | ⚠️ 只用 `StatusColorConverter` 显示文本颜色，**无图标** |
| 多阶段进度（蓝/橙/紫三色条） | ❌ |
| 文件覆盖对话框（覆盖/跳过/重命名/取消） | ❌ `FileService.GenerateOutputPath` 总是重命名，**无对话框** |
| 可折叠面板 | ❌ 改为 TabControl |
| 确认对话框 | ❌ |
| 多语言（.resx） | ❌ 字符串硬编码 |
| 队列状态持久化 | ❌ |
| 临时文件清理 | ❌ |
| 性能监控实时刷新 | ❌ |

### 2.8 部署与打包

| Spec 要求 | 现状 |
|---|---|
| `Package.appxmanifest` (MSIX) | ❌ |
| 自包含 EXE / `dotnet publish` 配置 | ❌ |
| `app.db` / `models/whisper/` / `models/onnx/` / `logs/` 目录 | ❌（仅 `app.log` 由 `LoggingService` 写入 `%APPDATA%\LemonSubtitleStudio\app.log`） |

### 2.9 安全 / NuGet

| 项 | 状态 |
|---|---|
| 输入路径校验、白名单 | ❌（`FileService` 不验证） |
| NuGet 全部依赖 | ✅ 7 个核心包都已添加 |

---

## 三、按优先级汇总的"待办缺口"

### P0 - 核心业务逻辑（项目无法真正工作）

1. **实现 `IAudioService` + `AudioService`**：用 `Xabe.FFmpeg` 抽音、转 WAV 16kHz。
2. **实现 `ITranscriptionService` + `TranscriptionService`**：用 `Whisper.net` 加载模型、转字幕、报告进度、支持语言与精度。
3. **实现 `ITranslationService` + `TranslationService`**：用 `Microsoft.ML.OnnxRuntime` 跑翻译模型。
4. **改造 `TaskQueueService.ExecuteTaskAsync`**：串联 Audio → Transcription → (Translation)，支持多阶段进度、取消、错误重试。
5. **5 个 ViewModel 的 `StartProcessing`** 改为调用真实服务：删除 `Task.Delay` 占位循环。
6. **字幕文件 I/O**：SRT/VTT 解析与保存、覆盖策略对话框（spec 7.4.5）。

### P1 - 关键交互

7. **视频/音频播放控制**：`SubtitleEditorViewModel` 暴露 `Play/Pause/Stop/Seek`，`MediaElement` 绑定 `Source`，按钮接 Command。
8. **页面间跳转传参**：从视频转字幕 / 音频转字幕的"编辑"按钮 → 跳 `SubtitleEditorView` 并携带 `MediaPath` + `SubtitlePath`（用 `INavigationAware` 或 `RegionContext`）。
9. **完成后双击预览**：文件列表 `MouseDoubleClick` → 写 `MediaPlayer.Source`。
10. **设置页模型管理 Tab**：列出本地模型 + 下载/删除/设为默认，调用 `ModelManagerService` 完善。
11. **命名规则 + 默认输出目录联动**：所有页面的 `OutputDirectory` 在 ViewModel 构造时从 `ISettingsService.DefaultOutputDirectory` 取值（已部分实现，需保证）。

### P2 - 数据 / 可视化

12. **SQLite + `IHistoryService`**：建 `AppDbContext` + `HistoryRepository`，每次任务完成/失败写入。
13. **重试机制**：UI 上失败状态显示"重试"按钮 → 重新入队。
14. **性能监控后台采样**：`System.Threading.PerformanceCounter` 1 秒刷新 `MemoryUsage/CpuUsage`。
15. **音频波形可视化**（可降级）：占位 `TextBlock` → `Canvas` 绘制或 OxyPlot / NAudio。

### P3 - 体验与打包

16. **多语言 `.resx`**：替换硬编码字符串，UI 即时切换。
17. **队列状态持久化**：每添加/删除 + 5 秒自动保存到 `app.db`。
18. **临时文件清理**：任务完成 / 应用退出时清中间文件。
19. **MSIX 打包**：`Package.appxmanifest` + `dotnet publish`。
20. **文件覆盖对话框**（spec 7.4.5）。
21. **配置对话框 / 确认对话框**（删除、清空、取消任务）。

### 架构层面缺口

22. 拆分出 `LemonSubtitleStudio.Core`（Services + Models）和 `LemonSubtitleStudio.Infrastructure`（Wrappers + Data）。
23. 修正 `TaskStatus` 枚举值以匹配 spec。
24. `SubtitleRow` 改为字符串时间字段或保留 `TimeSpan` 但加双向转换（取决于最终选型）。
25. ViewModel 通过 DI 容器（Prism `IContainerProvider`）解析服务，而不是 `new`。

---

## 四、文件级"已实现 / 占位 / 缺失"一览

| 文件 | 状态 | 备注 |
|---|---|---|
| `App.xaml.cs` | 已实现 | 6 个页面 + 5 个 Service 注册到 DI |
| `Models/TaskItem.cs` | 已实现（结构偏离 spec） | 枚举 4 值，缺 `Extracting/Transcribing/Translating/Error` |
| `Models/SubtitleItem.cs` | 已实现（结构偏离 spec） | 时间用 `TimeSpan`，spec 用字符串 |
| `Services/FileService.cs` | 已实现 | 文件/文件夹选择、生成输出路径 |
| `Services/SettingsService.cs` | 已实现 | 存 `settings.xml`（应改为 `app.db`） |
| `Services/ModelManagerService.cs` | 已实现 | Whisper 模型下载（无删除/默认管理） |
| `Services/TaskQueueService.cs` | 占位 | `ExecuteTaskAsync` 是 `Task.Delay` 伪进度 |
| `Services/LoggingService.cs` | 已实现 | |
| `Services/IAudioService.cs` | **缺失** | |
| `Services/AudioService.cs` | **缺失** | Xabe.FFmpeg 已就绪但未引用 |
| `Services/ITranscriptionService.cs` | **缺失** | |
| `Services/TranscriptionService.cs` | **缺失** | Whisper.net 已就绪但未引用 |
| `Services/ITranslationService.cs` | **缺失** | |
| `Services/TranslationService.cs` | **缺失** | |
| `Services/IHistoryService.cs` | **缺失** | |
| `Services/HistoryService.cs` | **缺失** | |
| `Data/AppDbContext.cs` | **缺失** | SQLite 包已安装但未使用 |
| `Data/HistoryRepository.cs` | **缺失** | |
| `Views/ShellView.xaml` | 已实现 | 6 项导航 + 语言切换 + 版本号 |
| `Views/VideoToSubtitleView.xaml` | 部分实现 | 4 个 Tab 替代可折叠面板，**视频预览未绑定 Source** |
| `Views/VideoToAudioView.xaml` | 部分实现 | **波形/播放均为占位** |
| `Views/AudioToSubtitleView.xaml` | 部分实现 | 同上 |
| `Views/SubtitleTranslationView.xaml` | 部分实现 | 双语 DataGrid 已有，但**绑定到同一个 `Subtitles`，译文永远空** |
| `Views/SubtitleEditorView.xaml` | 占位 | `MediaElement` + 按钮**无 Command 绑定** |
| `Views/SettingsView.xaml` | 部分实现 | 缺模型管理 Tab / 命名规则 / 恢复默认 |
| `ViewModels/*` | 全部为占位 | 全部 `StartProcessing` 是伪进度；跳转逻辑未实现；未走 DI |
| `Converters/*` | 已实现 | |
| `Resources/Styles.xaml` | 已实现 | |

---

## 五、关键差距图示（核心业务流程 vs 现状）

```
[用户拖入视频]  →  StartProcessing()
                     │
                     ├─ VideoToSubtitleViewModel.StartProcessing
                     │     └─ 循环 Task.Delay(200) 推进伪进度  ❌ 应为：
                     │        1) AudioService.ConvertToWavAsync
                     │        2) TranscriptionService.TranscribeAsync
                     │        3) 写 SRT/VTT
                     │        4) 翻译（如勾选）
                     │
                     ├─ VideoToAudioViewModel.StartProcessing
                     │     └─ 伪进度  ❌ 应为：AudioService.ExtractAudioAsync
                     │
                     ├─ AudioToSubtitleViewModel.StartProcessing
                     │     └─ 伪进度  ❌ 应为：TranscriptionService
                     │
                     ├─ SubtitleTranslationViewModel.StartProcessing
                     │     └─ 伪进度  ❌ 应为：TranslationService
                     │
                     └─ 字幕[编辑]按钮
                           └─ 仅写日志  ❌ 应为：导航到 SubtitleEditorView
                                        并通过 NavigationParameters 传 MediaPath/SubtitlePath
```

---

## 六、建议执行顺序

1. **补齐 P0 服务层**（Audio / Transcription / Translation），DI 改造所有 ViewModel。
2. **重写 5 个业务 ViewModel 的处理流程**，接真实服务 + 进度上报 + 错误重试。
3. **字幕文件 I/O**（解析 / 保存 / 覆盖对话框）。
4. **页面间跳转 + 字幕编辑器播放控制**。
5. **SQLite + 历史记录 + 性能监控后台采样**。
6. **设置页模型管理 Tab + 命名规则**。
7. **多语言 .resx + MSIX 打包**。

> 阶段性验证：完成 1→2 后项目即可"端到端跑通"；完成 3→5 后达到可用 MVP；6→7 后达到发布状态。

---

## 七、附录：仓库中已存在的辅助产物

- `stitch_lemon_subtitle_studio/`：6 个 HTML 页面原型 + 截图（`video_subtitle / video_audio / audio_subtitle / subtitle_translation / subtitle_editor / settings`），可作为 UI 还原参考。
- `.trae/documents/lemon_subtitle_studio_completion_plan.md`：与本报告同源的完成计划，列出了 P0-P3 阶段任务，本报告与之互补——本报告聚焦"**实现状态盘点**"，该文档聚焦"**实现步骤**"。
- `stitch.md`：可能是 Stitch 平台导出的额外说明（未读取以避免无关内容干扰）。

---

*本报告基于 `specfornet.md` 与 `LemonSubtitleStudio/` 下的 C# / XAML 源码逐项比对得出，结论性陈述均可在上述文件路径找到证据。*
