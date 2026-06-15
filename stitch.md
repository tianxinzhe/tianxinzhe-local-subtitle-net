# Lemon Subtitle Studio — DESIGN.md

> **DESIGN.md for Google Stitch**  
> 将以下内容导入 Stitch（stitch.withgoogle.com）以生成 Lemon Subtitle Studio 的 UI 设计。

---

## Project Overview

**Project:** Lemon Subtitle Studio  
**Platform:** Windows Desktop (WPF)  
**Design Vibe:** Modern, clean, professional — 柠檬黄点缀的深色工具风格，强调视频/音频处理的专业感和高效 workflow

**Core Workflow (4 paths):**
- Video → Subtitle (video to subtitle extraction + ASR)
- Video → Audio (video to audio extraction)
- Audio → Subtitle (audio to subtitle via Whisper ASR)
- Subtitle → Translation (subtitle translation)

---

## Design Tokens

### Colors

```yaml
primary:
  base: "#FFA500"        # 柠檬橙 — 品牌色，用于按钮、激活状态、高亮
  hover: "#FF8C00"       # 更深橙色，用于 hover 状态
  light: "#FFE4B5"       # 浅橙色，用于背景填充

background:
  canvas: "#1A1A2E"     # 深色画布背景
  surface: "#16213E"     # 卡片/面板背景
  surface_alt: "#0F3460" # 次要面板/侧边栏背景
  elevated: "#1A1A3E"    # 浮动/弹窗背景

text:
  primary: "#FFFFFF"     # 主文字
  secondary: "#A0A0B0"  # 次要文字
  disabled: "#505060"   # 禁用态文字
  accent: "#FFA500"      # 强调文字

border:
  default: "#2A2A4A"    # 默认边框
  focus: "#FFA500"      # 聚焦边框

status:
  success: "#00C853"    # 任务完成绿色
  error: "#FF1744"      # 任务失败红色
  processing: "#448AFF" # 处理中蓝色
  idle: "#505060"       # 空闲状态灰色
```

### Typography

```yaml
font_family: "Segoe UI, -apple-system, sans-serif"

sizes:
  heading: 20px         # 页面标题
  subheading: 16px      # 区域标题
  body: 14px            # 正文
  caption: 12px         # 辅助文字
  badge: 10px           # 状态标签

weights:
  bold: 700
  semibold: 600
  regular: 400
```

### Spacing

```yaml
unit: 4px               # 基础单位
padding:
  compact: 8px
  normal: 16px
  loose: 24px
gap:
  small: 8px
  medium: 12px
  large: 16px
```

### Corners & Effects

```yaml
radius:
  small: 4px
  medium: 8px
  large: 12px
  full: 9999px

shadow:
  card: "0 2px 8px rgba(0,0,0,0.3)"
  elevated: "0 4px 16px rgba(0,0,0,0.4)"
```

---

## Layout Structure

### Main Window

```
┌─────────────────────────────────────────────────────────┐
│ ┌──────────┐  ┌──────────────────────────────────────┐ │
│ │  Nav     │  │           Main Content Area           │ │
│ │ Sidebar  │  │                                        │ │
│ │ 240px    │  │  (Prism Region — dynamic page swap)    │ │
│ │          │  │                                        │ │
│ └──────────┘  └──────────────────────────────────────┘ │
└─────────────────────────────────────────────────────────┘
```

### Navigation Sidebar (240px fixed width)

| Section | Element | Description |
|---------|---------|-------------|
| Top | Logo | "Lemon Subtitle Studio" + lemon icon |
| | GPU/CPU Status Badge | Small badge showing compute mode |
| Middle | Navigation Items (6) | Vertical button group |
| | 1. 🎬 Video → Subtitle | Video to subtitle extraction |
| | 2. 🎵 Video → Audio | Video to audio extraction |
| | 3. 🎙️ Audio → Subtitle | Audio to subtitle via ASR |
| | 4. 🌐 Subtitle Translation | Translate subtitles |
| | 5. ✏️ Subtitle Editor | Edit subtitles with video |
| | 6. ⚙️ Settings | App configuration |
| Bottom | Language Selector | EN/中文 toggle dropdown |
| | Version Info | Version number + thread count |

### Common Page Template (for pages 1-4)

Each functional page follows this consistent template:

```
┌──────────────────────────────────────────────────────────────┐
│  [Page Title]  [X files]           [Export All] [Start]      │ ← Header bar
├──────────────────────────────────────────────────────────────┤
│  [Config Bar: language/model/format/output dir selector]     │ ← Config toolbar
├──────────────────────────────────────────────────────────────┤
│  ┌─ File Queue (320px) ──┐  ┌─ Right Panel (flex) ───────┐ │
│  │  Drop zone (drag/click)│  │  Waveform / Video Preview  │ │
│  │  File list             │  │  Playback Controls         │ │
│  │  [progress][status]    │  │  Subtitle List / Progress  │ │
│  └────────────────────────┘  │  Processing Log            │ │
│                              └────────────────────────────┘ │
└──────────────────────────────────────────────────────────────┘
```

---

## Page-by-Page Design

### 1. Video → Subtitle Page

**Vibe:** 视频处理工作台 — 专业、直观

**Components:**
- **Header:** "Video → Subtitle" title, file count badge, performance stats (Memory/CPU), [Export All] [Start Processing] buttons
- **Config Bar:** Language dropdown (CN/EN/JA/KR), Precision toggle (High Speed / High Accuracy), Model selector, Output directory picker
- **Left Panel (File Queue, draggable width 200-400px):**
  - Drag & drop zone with dashed border + upload icon (highlight animation on drag over)
  - File list items with filename, format icon, progress bar, status dot
  - **Completed files show "Double-click to preview" hint**
  - Vertical scroll for overflow
- **Right Panel:**
  - **Video preview (only after completion):** MediaElement player with seek bar, play/pause controls
  - **Subtitle list:** timestamp | original text | [Edit] button
  - **Task progress:** Multi-stage progress bars (audio extraction → ASR recognition → translation)
  - **Processing log:** Auto-scroll, clear button

**States:**
- Empty: Show large drag zone with "Drop video files here" prompt
- Processing: Show progress, disable [Start] button, show cancel option, video preview disabled
- Done: Show green checkmark per completed file, enable [Export All], **double-click file to preview**, [Edit] button appears in subtitle list

**Key Interactions:**
- **Preview:** Double-click completed file → video preview plays in right panel
- **Edit Subtitle:** Click [Edit] button → **navigate to Subtitle Editor page** with video and subtitle paths passed

---

### 2. Video → Audio Page

**Vibe:** 简洁的音频提取工具

**Components:**
- **Header:** "Video → Audio" title, file count, performance stats (Memory/CPU), [Export All] [Extract]
- **Config Bar:** Format selector (WAV/MP3 toggle), Bitrate slider (192-320kbps for MP3), Output directory picker
- **Left Panel (File Queue, draggable width 200-400px):** Same drag & drop + file list pattern
- **Right Panel (Collapsible sections):**
  - Audio waveform visualization (animated, shows selected file)
  - Playback controls (play/pause/stop)
  - Task progress (current file, extraction progress bar)
  - Processing log

---

### 3. Audio → Subtitle Page

**Vibe:** 语音转字幕 — 智能精准

**Components:**
- **Header:** "Audio → Subtitle" title, file count, performance stats (Memory/CPU), [Export All] [Start Recognition]
- **Config Bar:** Language selector, Precision toggle, Model selector, Output directory picker
- **Left Panel (File Queue, draggable width 200-400px):** Same drag & drop + file list pattern
- **Right Panel:**
  - **Subtitle list:** timestamp | original text | [Edit] button (no waveform preview needed)
  - **Task progress:** Recognition progress bar
  - **Processing log:** Auto-scroll, clear button

**Key Interactions:**
- **No waveform preview:** This page does NOT include audio waveform visualization
- **No inline editing:** Subtitle list is read-only on this page
- **Edit Subtitle:** Click [Edit] button → **navigate to Subtitle Editor page** with audio and subtitle paths passed

---

### 4. Subtitle Translation Page

**Vibe:** 双语翻译工作台 — 对比编辑

**Components:**
- **Header:** "Subtitle Translation" title, file count, [Export All] [Translate]
- **Config Bar:** Model selector, Source → Target language pair selector, Output directory picker
- **Left Panel (File Queue, 320px):** Accepts .srt, .vtt, .ass files
- **Right Panel:**
  - Source subtitle (read-only, timestamp | original text)
  - Target subtitle (editable input fields, timestamp | translated text)
  - Task progress (translation progress bar)
  - Processing log

### 5. Subtitle Editor Page

**Vibe:** 媒体+字幕同步编辑器 — 边看边改

**Components:**
- **Header:** "Subtitle Editor" title, [Import Media] [Import Subtitle] [Export]
- **Top Section (Preview Area):**
  - **Auto-switch based on media type:**
    - **Video mode:** Video display area (16:9 aspect ratio) with playback controls
    - **Audio mode:** Audio waveform visualization with playback controls
  - Playback controls: [Play/Pause] [Rewind/Forward], progress bar with time display
  - Shows current subtitle text overlay (on video) or below waveform
- **Middle Section (Timeline):**
  - Horizontal timeline showing all subtitle segments as draggable blocks
  - Each block shows start/end time markers
  - Drag to adjust timing, split/merge capability
- **Bottom Section (Editor):**
  - Left: Original text editor (editable, with start/end time adjust buttons)
  - Right: Translation text editor (editable)
- **Bottom Bar:**
  - Subtitle list table (index | timestamp | original summary | translation summary)
  - Double-click to edit, click to sync playback

**Key Interactions:**
- **Media Type Auto-switch:** Automatically switches between video player and audio waveform based on imported media type
- **External Navigation Support:** Receives media file path and subtitle file path when navigated from other pages (Video → Subtitle or Audio → Subtitle)

---

### 6. Settings Page

**Vibe:** 配置面板 — 清晰易用

**Components:**
- **Header:** "Settings"
- **Model Management Section:**
  - Tab switch: [Speech Recognition Models] [Translation Models]
  - Model list cards: status dot (downloaded/not), model name, size, action buttons [Download] [Set Default] [Delete]
  - Tooltip/info box for model details
- **Storage Section:**
  - Model storage path: [text input] [Browse] — stores Whisper model files
  - **Default output directory:** [text input] [Browse] — **used as initial value for output directory pickers in all functional pages**
- **Subtitle Naming Section:**
  - Radio buttons: [Keep Original] [By Language] [Custom]
  - Custom format input field with placeholder: `{name}_{lang}.srt`
- **Bottom Action:**
  - [Restore Default Settings] button with confirmation dialog

---

## Interactive Behaviors

### Basic Interactions

| Action | Response |
|--------|----------|
| Drop file on drag zone | **Dashed border highlight animation**, shows accept state |
| Click drag zone | Native file picker dialog opens |
| Click [Start] | Button becomes disabled, shows spinning indicator, progress bars appear |
| Task completes | Green checkmark, success message |
| Task fails | Red X mark, error message in log + **Retry button** |
| Click file in queue | Right panel switches to show that file's details (preview/progress) |
| Right-click file in queue | Context menu: [Remove] [Clear Completed] |
| Click subtitle list item | Video/audio seeks to that timestamp |
| Log update | Auto-scroll to newest entry, [Clear] button available |
| Hover nav item | Background highlight + tooltip |
| Active nav item | Orange left border indicator + brighter text |

### Task States

| State | Visual | Available Actions |
|-------|--------|-------------------|
| Waiting | Gray dot | Remove |
| Processing | Blue spinner | Cancel |
| Completed | Green checkmark | Remove, Export |
| Failed | Red X | **Retry**, Remove |

### Performance Monitoring

Display real-time performance stats in the header bar:
- **Memory usage**: `Memory: XXXMB` (update every 1 second)
- **CPU usage**: `CPU: XX%` (update every 1 second)

```
┌──────────────────────────────────────────────────────────────────────┐
│  Video → Subtitle  [3 files]  [Memory: 256MB] [CPU: 12%]  [Export All] [Start Processing] │
└──────────────────────────────────────────────────────────────────────┘
```

### Multi-Stage Progress

For Video → Subtitle task, show progress in stages:
1. **Audio Extraction**: Blue progress bar
2. **Subtitle Recognition**: Orange progress bar
3. **Subtitle Translation**: Purple progress bar (optional)

### Collapsible Panels

Right panel areas are collapsible:
- Click header bar or collapse button to toggle
- Some areas (like video preview) can collapse but not to zero height

```
┌───────────────────────────────────────────────────────┐
│ ▼ Video Preview         [Height: Fixed] [Collapse]  │
│ ▼ Subtitle List         [Height: Auto]   [Collapse]  │
│ ▶ Task Progress         [Currently Hidden]           │
│ ▶ Processing Log        [Currently Hidden]           │
└───────────────────────────────────────────────────────┘
```

### File Overwrite Dialog

When output file already exists, show dialog:

```
┌──────────────────────────────────────────────────┐
│               File Already Exists                  │
├──────────────────────────────────────────────────┤
│  The following files already exist in output:     │
│                                                   │
│  ☑ video1.srt                                    │
│  ☑ video2.srt                                    │
│                                                   │
│  [ ] Apply to all remaining files                │
│                                                   │
│  [Overwrite] [Skip] [Rename] [Cancel]           │
└──────────────────────────────────────────────────┘
```

| Option | Behavior |
|--------|----------|
| Overwrite | Replace existing file |
| Skip | Skip this file, continue to next |
| Rename | Auto-add numeric suffix (e.g., `video1_1.srt`) |
| Cancel | Cancel all file processing |

### Confirmation Dialogs

For irreversible operations, show confirmation dialog:

| Operation | Confirmation Message |
|-----------|---------------------|
| Delete file | "Are you sure you want to remove this file from the queue?" |
| Clear queue | "Are you sure you want to clear all files? This action cannot be undone." |
| Cancel task | "Are you sure you want to cancel the current task?" |

### Queue State Persistence

| State | Save Timing | Recovery |
|-------|-------------|----------|
| Queue file list | On each add/remove | Auto-restore on startup |
| Task progress | Save every 5 seconds | Option to restore or restart |
| User settings | Immediate save | Auto-load on startup |

### Temporary File Cleanup

| Timing | Cleanup Action |
|--------|----------------|
| Task completed | Auto-delete temp audio/intermediate files |
| App closed | Clean up all incomplete temp files |
| Exception | User can set to keep temp files for debugging |

---

## Screen Prototype Notes

**Flow (Video to Subtitle as example):**
1. User opens app → sees empty Video → Subtitle page by default (or last-used page)
2. Drops 3 MP4 files into the queue → files appear with filenames, format icons, gray status dots
3. Selects language (Chinese), mode (High Accuracy), picks Whisper Base model, sets output dir
4. Clicks [Start Processing] → buttons disable, header shows real-time Memory/CPU stats, first file shows multi-stage progress
5. File 1 completes → green check, auto-starts File 2, log shows success message
6. If File 2 fails → red X mark, Retry button appears, user can click Retry or continue to File 3
7. All done → [Export All] becomes active, if output files exist, overwrite dialog appears
8. User can drag queue edge to adjust width (min 200px, max 400px)

**Export format options (global):** .srt, .vtt, .ass, .txt

**Multi-language Interface:**
- Language selector in sidebar bottom: 中文/EN/日本語 etc.
- Uses `.resx` resource files to manage interface text
- Dynamic switching without restart
