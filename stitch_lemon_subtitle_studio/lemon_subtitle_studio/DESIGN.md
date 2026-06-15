---
name: Lemon Subtitle Studio
colors:
  surface: '#111125'
  surface-dim: '#111125'
  surface-bright: '#37374d'
  surface-container-lowest: '#0c0c1f'
  surface-container-low: '#1a1a2e'
  surface-container: '#1e1e32'
  surface-container-high: '#28283d'
  surface-container-highest: '#333348'
  on-surface: '#e2e0fc'
  on-surface-variant: '#d9c3ac'
  inverse-surface: '#e2e0fc'
  inverse-on-surface: '#2f2e43'
  outline: '#a18e79'
  outline-variant: '#534433'
  surface-tint: '#ffb95c'
  primary: '#ffcb8c'
  on-primary: '#462a00'
  primary-container: '#ffa500'
  on-primary-container: '#684000'
  inverse-primary: '#855400'
  secondary: '#a9c8fc'
  on-secondary: '#09305c'
  secondary-container: '#294a77'
  on-secondary-container: '#9bbaee'
  tertiary: '#98ddff'
  on-tertiary: '#003546'
  tertiary-container: '#02c7ff'
  on-tertiary-container: '#004f68'
  error: '#ffb4ab'
  on-error: '#690005'
  error-container: '#93000a'
  on-error-container: '#ffdad6'
  primary-fixed: '#ffddb7'
  primary-fixed-dim: '#ffb95c'
  on-primary-fixed: '#2a1700'
  on-primary-fixed-variant: '#653e00'
  secondary-fixed: '#d5e3ff'
  secondary-fixed-dim: '#a9c8fc'
  on-secondary-fixed: '#001b3c'
  on-secondary-fixed-variant: '#274774'
  tertiary-fixed: '#bee9ff'
  tertiary-fixed-dim: '#6bd3ff'
  on-tertiary-fixed: '#001f2a'
  on-tertiary-fixed-variant: '#004d65'
  background: '#111125'
  on-background: '#e2e0fc'
  surface-variant: '#333348'
typography:
  h1:
    fontFamily: Segoe UI
    fontSize: 20px
    fontWeight: '600'
    lineHeight: 28px
  h2:
    fontFamily: Segoe UI
    fontSize: 16px
    fontWeight: '600'
    lineHeight: 24px
  body-md:
    fontFamily: Segoe UI
    fontSize: 14px
    fontWeight: '400'
    lineHeight: 20px
  body-sm:
    fontFamily: Segoe UI
    fontSize: 12px
    fontWeight: '400'
    lineHeight: 16px
  label-caps:
    fontFamily: Segoe UI
    fontSize: 11px
    fontWeight: '700'
    lineHeight: 16px
    letterSpacing: 0.05em
  mono:
    fontFamily: Cascadia Code
    fontSize: 13px
    fontWeight: '400'
    lineHeight: 18px
rounded:
  sm: 0.25rem
  DEFAULT: 0.5rem
  md: 0.75rem
  lg: 1rem
  xl: 1.5rem
  full: 9999px
spacing:
  unit: 4px
  xs: 4px
  sm: 8px
  md: 16px
  lg: 24px
  xl: 32px
  sidebar_width: 240px
  header_height: 56px
---

## Brand & Style
The design system focuses on a high-productivity, professional utility aesthetic tailored for long-duration focus. It combines a **Corporate Modern** foundation with subtle **Glassmorphic** influences to provide depth without sacrificing the performance-first feel of a Windows desktop application.

The visual narrative is "Precision through Clarity." By utilizing a deep navy palette paired with high-energy "Lemon Orange" accents, the system directs user attention toward critical action paths and active states, reducing cognitive load during complex subtitle editing tasks. The emotional response is one of reliability, technical mastery, and modern efficiency.

## Colors
The color system is optimized for a dark-room environment. 
- **Canvas:** Use the deepest navy for the main application window background.
- **Surfaces:** Use `Surface Default` for primary containers and `Surface Alt` for interactive regions or distinct workspace sections (e.g., sidebar).
- **Accents:** The Lemon Orange is reserved exclusively for primary calls-to-action, progress indicators, and active selection states. 
- **Status:** Functional colors follow standard semantic expectations but are tuned for high legibility against dark backgrounds.

## Typography
The system utilizes **Segoe UI** to maintain a native Windows feel while ensuring maximum readability. 
- **Headlines:** Use Semi-Bold weights for section headers to provide clear hierarchy in the sidebar and workspace.
- **Body:** The 14px base size ensures density for data-heavy subtitle lists while remaining legible.
- **Labels:** Use uppercase for utility labels (e.g., timestamps) to distinguish them from editable text content.
- **Monospaced:** A secondary monospaced font is recommended for timecode editing to prevent layout jitter during value changes.

## Layout & Spacing
The layout follows a structured, multi-pane approach characteristic of professional Windows utilities.
- **Sidebar:** Fixed at 240px. Contains primary navigation and project-level folders.
- **Workspace:** A split-pane model. The left pane (File Queue) should occupy 30-40% of the horizontal space, with the right pane (Preview/Details) taking the remainder.
- **Gaps:** Use 16px (md) for general container padding and 8px (sm) for internal component spacing. 
- **Alignment:** All elements must align to the 4px baseline grid to ensure a crisp, engineered look.

## Elevation & Depth
Depth is created through **Tonal Layering** and refined **Inner Strokes** rather than heavy shadows.
- **Level 0 (Canvas):** The base layer (#1A1A2E).
- **Level 1 (Panels):** Raised using `Surface Default` with a 1px solid border of `Border-Default`.
- **Level 2 (Modals/Popovers):** Highest elevation. These use `Surface Alt`, a slightly lighter border, and a subtle 8px blur shadow with 20% opacity.
- **Interactions:** Hover states on list items should use a subtle background tint change rather than a shadow to maintain the "flat-plus" professional aesthetic.

## Shapes
The design system uses a consistent **8px (0.5rem)** radius for all primary containers, buttons, and input fields. This provides a modern, approachable feel while retaining the structural integrity of a technical tool. 
- Larger components like the main workspace cards may use `rounded-lg` (16px) to soften the overall UI.
- Selection indicators in the sidebar utilize a "capsule" or vertical bar on the left edge to denote focus.

## Components
- **Buttons:** Primary buttons are solid `Primary-Base` with white text. Secondary buttons are outlined with `Border-Default` and transition to a subtle navy fill on hover.
- **Input Fields:** Use `Surface-Alt` for the background. The bottom border or full outline should switch to `Primary-Base` on focus.
- **File Queue Items:** Lists should support alternating row colors (zebra striping) using a 2% opacity difference for high-density readability.
- **Scrollbars:** Use a slim, "ghost" style scrollbar that matches the `Border-Default` color to prevent visual clutter in the panes.
- **Progress Bars:** Use a thin 4px track. The fill is `Primary-Base` for active tasks and `Status-Success` upon completion.
- **Splitters:** The dividers between panes should be interactive, 4px wide on hover, and use the `Border-Default` color.