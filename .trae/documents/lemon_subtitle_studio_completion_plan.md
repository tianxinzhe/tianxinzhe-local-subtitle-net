# Lemon Subtitle Studio 完成计划

## 概述

本文档详细规划了 Lemon Subtitle Studio 剩余功能的实现步骤，确保项目从当前框架状态达到完整可用状态。

---

## 当前状态分析

### ✅ 已完成

| 模块 | 文件 | 状态 |
|------|------|------|
| 项目结构 | `LemonSubtitleStudio.sln`, `.csproj` | ✅ 完成 |
| 导航框架 | `ShellView.xaml`, `ShellViewModel.cs` | ✅ 完成 |
| 全局样式 | `Resources/Styles.xaml` | ✅ 完成 |
| 6个页面UI | `Views/*.xaml`, `ViewModels/*.cs` | ✅ 框架完成 |
| 设置服务 | `SettingsService.cs` | ✅ 完成 |
| 模型管理服务 | `ModelManagerService.cs` | ✅ 完成 |
| 日志服务 | `LoggingService.cs` | ✅ 完成 |
| 文件选择服务 | `FileService.cs` | ✅ 完成 |
| 任务队列服务 | `TaskQueueService.cs` | ⚠️ 框架存在，无实际处理逻辑 |
| 数据模型 | `TaskItem.cs`, `SubtitleItem.cs` | ✅ 完成 |
| 转换器 | `TaskStatusConverter.cs`, `StatusColorConverter.cs` | ✅ 完成 |

### ❌ 未实现

| 服务 | 规格定义 | 优先级 |
|------|---------|--------|
| `IAudioService` | 音频提取、格式转换 | P0 - 最高 |
| `ITranscriptionService` | Whisper语音识别 | P0 - 最高 |
| `ITranslationService` | ONNX翻译模型 | P1 - 高 |
| `IHistoryService` | 历史记录管理 | P2 - 中 |
| SQLite数据库 | 数据持久化 | P2 - 中 |
| 视频播放控制 | MediaElement控制逻辑 | P1 - 高 |
| 波形显示 | 音频波形可视化 | P2 - 中 |
| 国际化 | .resx资源文件 | P3 - 低 |

---

## 实现计划

### 阶段一：核心服务层 (P0)

#### 1.1 音频服务 (AudioService)

**文件**: `Services/AudioService.cs`, `Services/IAudioService.cs`

**功能**:
- 从视频文件提取音频轨道
- 音频格式转换 (任意格式 → WAV 16kHz)
- FFmpeg 可用性检测

**实现要点**:
```csharp
public interface IAudioService
{
    Task<string> ExtractAudioAsync(string inputPath, string outputDir, string format = "mp3", int bitrate = 192);
    Task<string> ConvertToWavAsync(string inputPath, string outputDir, int sampleRate = 16000);
    bool IsFfmpegAvailable();
}
```

**依赖**: Xabe.FFmpeg (已在csproj中)

---

#### 1.2 语音识别服务 (TranscriptionService)

**文件**: `Services/TranscriptionService.cs`, `Services/ITranscriptionService.cs`

**功能**:
- 使用 Whisper.net 进行语音识别
- 支持多种语言 (中文、英文、日语、韩语)
- 支持高精度/快速两种模式
- GPU加速支持 (DirectML/CUDA)

**实现要点**:
```csharp
public interface ITranscriptionService
{
    Task<List<SubtitleItem>> TranscribeAsync(string audioPath, string language, bool precisionMode, IProgress<int> progress);
    Task<bool> IsModelAvailable(string modelName);
    Task InitializeModelAsync(string modelName);
}
```

**依赖**: Whisper.net, Whisper.net.Runtime, Microsoft.ML.OnnxRuntime.DirectML (已在csproj中)

---

#### 1.3 任务处理逻辑完善

**文件**: `Services/TaskQueueService.cs` (修改)

**功能**:
- 集成 AudioService 和 TranscriptionService
- 实现多阶段进度报告 (音频提取 → 语音识别 → 翻译)
- 支持任务取消

**修改要点**:
- 注入 `IAudioService` 和 `ITranscriptionService`
- 实现 `ExecuteTaskAsync` 的真实处理逻辑
- 添加进度回调机制

---

### 阶段二：功能页面完善 (P1)

#### 2.1 视频转字幕页面增强

**文件**: `ViewModels/VideoToSubtitleViewModel.cs`, `Views/VideoToSubtitleView.xaml`

**修改内容**:
1. 注入真实服务 (`IAudioService`, `ITranscriptionService`)
2. 实现真实的 `StartProcessing()` 方法
3. 添加视频预览控制逻辑
4. 实现双击预览功能
5. 实现编辑按钮跳转到字幕编辑页面

**关键代码修改**:
```csharp
// VideoToSubtitleViewModel.cs
private async void StartProcessing()
{
    foreach (var task in Tasks)
    {
        // 1. 提取音频
        var audioPath = await _audioService.ConvertToWavAsync(task.InputPath, OutputDirectory);

        // 2. 语音识别
        var subtitles = await _transcriptionService.TranscribeAsync(
            audioPath, SelectedLanguage, SelectedProcessingMode == "高精度", progress);

        // 3. 保存字幕文件
        // ...
    }
}
```

---

#### 2.2 视频转音频页面增强

**文件**: `ViewModels/VideoToAudioViewModel.cs`, `Views/VideoToAudioView.xaml`

**修改内容**:
1. 实现真实的音频提取功能
2. 添加格式选择 (WAV/MP3)
3. 添加比特率调节 (MP3)
4. 实现音频波形预览 (可选，P2)

---

#### 2.3 音频转字幕页面增强

**文件**: `ViewModels/AudioToSubtitleViewModel.cs`, `Views/AudioToSubtitleView.xaml`

**修改内容**:
1. 实现真实的语音识别功能
2. 支持批量音频文件处理
3. 实现编辑按钮跳转到字幕编辑页面

---

#### 2.4 字幕翻译页面增强

**文件**: `ViewModels/SubtitleTranslationViewModel.cs`, `Views/SubtitleTranslationView.xaml`

**修改内容**:
1. 创建 `ITranslationService` 接口和实现
2. 实现字幕翻译功能
3. 实现双语字幕编辑
4. 支持导出双语字幕

---

#### 2.5 字幕编辑页面增强

**文件**: `ViewModels/SubtitleEditorViewModel.cs`, `Views/SubtitleEditorView.xaml`

**修改内容**:
1. 实现视频播放控制 (播放/暂停/停止/跳转)
2. 实现字幕时间轴同步
3. 实现字幕时间调整
4. 支持从其他页面跳转并加载文件
5. 实现字幕保存功能 (SRT/VTT格式)

**视频播放控制实现**:
```csharp
// SubtitleEditorViewModel.cs
public void Play() => _mediaPlayer.Play();
public void Pause() => _mediaPlayer.Pause();
public void Stop() => _mediaPlayer.Stop();
public void Seek(TimeSpan position) => _mediaPlayer.Position = position;
```

---

### 阶段三：数据持久化 (P2)

#### 3.1 SQLite数据库集成

**文件**: `Data/AppDbContext.cs`, `Data/HistoryRepository.cs`

**功能**:
- 创建 SQLite 数据库上下文
- 实现历史记录存储
- 实现模型设置存储

**表结构**:
```sql
CREATE TABLE history_records (
    id INTEGER PRIMARY KEY AUTOINCREMENT,
    input_file TEXT NOT NULL,
    output_dir TEXT NOT NULL,
    language TEXT NOT NULL,
    status TEXT NOT NULL,
    created_at TEXT NOT NULL,
    output_files TEXT
);
```

---

#### 3.2 历史记录服务

**文件**: `Services/HistoryService.cs`, `Services/IHistoryService.cs`

**功能**:
- 记录处理历史
- 查询历史记录
- 删除历史记录

---

#### 3.3 波形显示 (可选)

**文件**: `Controls/WaveformControl.xaml`, `Controls/WaveformControl.xaml.cs`

**功能**:
- 音频波形可视化
- 支持缩放和时间标记

---

### 阶段四：国际化与优化 (P3)

#### 4.1 国际化支持

**文件**: `Resources/Strings.resx`, `Resources/Strings.zh-CN.resx`, `Resources/Strings.en-US.resx`

**功能**:
- 创建多语言资源文件
- 实现语言切换
- 更新所有UI文本绑定

---

#### 4.2 性能优化

- 内存使用监控
- CPU使用率监控
- 大文件处理优化

---

## 文件清单

### 新增文件

| 文件路径 | 说明 |
|---------|------|
| `Services/IAudioService.cs` | 音频服务接口 |
| `Services/AudioService.cs` | 音频服务实现 |
| `Services/ITranscriptionService.cs` | 语音识别服务接口 |
| `Services/TranscriptionService.cs` | 语音识别服务实现 |
| `Services/ITranslationService.cs` | 翻译服务接口 |
| `Services/TranslationService.cs` | 翻译服务实现 |
| `Services/IHistoryService.cs` | 历史记录服务接口 |
| `Services/HistoryService.cs` | 历史记录服务实现 |
| `Data/AppDbContext.cs` | SQLite数据库上下文 |
| `Data/HistoryRepository.cs` | 历史记录仓库 |
| `Resources/Strings.resx` | 默认语言资源 |
| `Resources/Strings.zh-CN.resx` | 中文资源 |
| `Resources/Strings.en-US.resx` | 英文资源 |

### 修改文件

| 文件路径 | 修改内容 |
|---------|---------|
| `Services/TaskQueueService.cs` | 集成真实服务，实现处理逻辑 |
| `ViewModels/VideoToSubtitleViewModel.cs` | 注入服务，实现真实处理 |
| `ViewModels/VideoToAudioViewModel.cs` | 实现音频提取功能 |
| `ViewModels/AudioToSubtitleViewModel.cs` | 实现语音识别功能 |
| `ViewModels/SubtitleTranslationViewModel.cs` | 实现翻译功能 |
| `ViewModels/SubtitleEditorViewModel.cs` | 实现播放控制和编辑功能 |
| `Views/*.xaml` | 绑定更新，添加功能控件 |
| `App.xaml.cs` | 注册新服务到DI容器 |

---

## 验证步骤

### 阶段一验证

1. 运行项目，检查无编译错误
2. 测试音频提取功能：选择视频文件，验证音频输出
3. 测试语音识别功能：选择音频文件，验证字幕生成

### 阶段二验证

1. 测试视频转字幕完整流程
2. 测试视频转音频完整流程
3. 测试音频转字幕完整流程
4. 测试字幕编辑器播放控制
5. 测试页面间跳转功能

### 阶段三验证

1. 验证历史记录保存和查询
2. 验证数据库文件创建

### 阶段四验证

1. 测试语言切换功能
2. 验证性能监控显示

---

## 风险与假设

### 假设

1. FFmpeg 已正确安装或 Xabe.FFmpeg 可自动下载
2. Whisper 模型可通过网络下载
3. 目标系统为 Windows 10 1809+ (DirectML支持)

### 风险

| 风险 | 缓解措施 |
|------|---------|
| FFmpeg 未安装 | 使用 Xabe.FFmpeg 自动下载功能 |
| GPU 不可用 | 回退到 CPU 模式 |
| 模型下载失败 | 提供离线模型导入功能 |
| 大文件内存溢出 | 分块处理，限制并发数 |

---

## 执行顺序

```
阶段一 (P0) → 阶段二 (P1) → 阶段三 (P2) → 阶段四 (P3)
     ↓              ↓              ↓              ↓
 AudioService    页面增强      SQLite        国际化
 Transcription   播放控制      History       优化
 TaskQueue完善   翻译功能      波形显示
```

建议按阶段顺序执行，每个阶段完成后进行验证测试。
