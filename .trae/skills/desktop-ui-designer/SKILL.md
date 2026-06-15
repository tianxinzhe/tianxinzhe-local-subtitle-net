---
name: "desktop-ui-designer"
description: "Provides WPF desktop UI design guidance including layout, styling, control selection, MVVM patterns, and XAML best practices. Invoke when user asks about UI design, layout advice, styling questions, control recommendations, or XAML implementation."
---

# 桌面端 UI 设计大师

为 WPF 桌面应用程序提供专业的 UI/UX 设计指导。本技能专注于 WPF + Prism + MVVM 架构下的界面设计最佳实践。

## 设计原则

### 1. 布局设计
- **使用 Grid 作为主布局容器**，避免过度嵌套，保持 XAML 清晰
- **合理利用行/列定义**，使用 `*` 比例尺寸、`Auto` 自适应、`Absolute` 固定尺寸
- **内容与容器分离**，DataTemplate 定义数据呈现方式，ItemsControl/ListView 承载列表
- **避免硬编码尺寸**，优先使用 `MinWidth`/`MaxWidth` 约束而非固定 Width
- **考虑 DPI 缩放**，使用 `Viewbox` 或动态尺寸适应不同分辨率

### 2. 样式与主题
- **使用 ResourceDictionary 管理全局样式**，避免内联样式泛滥
- **遵循 Prism 的 Regional 导航模式**，Region 内切换 View
- **统一使用 Material Design / Fluent Design 风格**，保持视觉一致性
- **使用 `StaticResource` 而非 `DynamicResource`** 提升性能（除非需要运行时换肤）

### 3. 控件选择指南
| 场景 | 推荐控件 | 理由 |
|------|---------|------|
| 文件列表（批量操作） | `ListBox` + 自定义 `ItemTemplate` | 轻量，支持多选 |
| 数据表格 | `DataGrid` | 内置排序、列调整、编辑 |
| 媒体预览 | `MediaElement` | WPF 原生媒体播放 |
| 标签/卡片切换 | `TabControl` | 原生支持 Tab 切换 |
| 可视化状态指示 | 自定义 UserControl + `Border`/`Rectangle` | 灵活控制视觉样式 |
| 目录/文件选择 | 调用 `FolderBrowserDialog`/`OpenFileDialog` | 原生体验 |

### 4. MVVM 最佳实践
- **View 只负责布局和绑定**，不包含业务逻辑
- **ViewModel 通过 `INotifyPropertyChanged` 驱动 UI 更新**
- **使用 `ICommand` 处理按钮点击**，而非 Code-behind 事件
- **Prism 的 `DelegateCommand`**：`ObservesProperty` / `ObservesCanExecute`
- **Dialog 交互通过 `IDialogService`**，避免 View 直接弹出窗口
- **文件拖拽等 UI 特定操作**可在 View 的 Code-behind 处理，通过 Binding 传递给 ViewModel

### 5. 本项目特定规范（Lemon Subtitle Studio）
- **主窗口布局**：左侧导航（6项平铺导航） + 右侧内容区（Region）
- **统一批量模式**：所有功能页面使用统一的批量处理模式，无单文件/批量切换
- **页面模板结构**：
  ```
  ┌─ 顶部标题栏 ─────────────────────────────────┐
  ├─ 工具配置栏（格式/参数/模型/语言/输出目录）───┤
  ├─ ┌─ 文件队列 ──┐ ┌─ 右侧面板（预览/进度）─┐ │
  │  │ 拖拽添加区域 │ │ 波形/字幕预览          │ │
  │  │ 文件列表     │ │ 播放控制/任务进度/日志 │ │
  │  └──────────────┘ └────────────────────────┘ │
  ├─ [导出全部] [开始处理] ──────────────────────┤
  └──────────────────────────────────────────────┘
  ```
- **文件队列**：左侧固定宽度 320px，ListBox 实现
- **右侧面板**：根据上下文动态展示波形预览/字幕列表/任务进度/处理日志

## 触发场景
当用户提出以下问题时自动调用此技能：
- "这个界面怎么布局比较好？"
- "帮我设计一下 XX 页面的布局"
- "这个控件应该用什么？"
- "WPF 里如何实现 XX 效果？"
- "XAML 样式/模板怎么写？"
- "MVVM 模式下如何实现 XX 交互？"
- 任何与 WPF 桌面 UI 设计相关的问题
