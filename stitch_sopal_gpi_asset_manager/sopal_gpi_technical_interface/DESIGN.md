---
name: SOPAL_GPI Technical Interface
colors:
  surface: '#10131a'
  surface-dim: '#10131a'
  surface-bright: '#363941'
  surface-container-lowest: '#0b0e15'
  surface-container-low: '#191b23'
  surface-container: '#1d2027'
  surface-container-high: '#272a31'
  surface-container-highest: '#32353c'
  on-surface: '#e1e2ec'
  on-surface-variant: '#c2c6d6'
  inverse-surface: '#e1e2ec'
  inverse-on-surface: '#2e3038'
  outline: '#8c909f'
  outline-variant: '#424754'
  surface-tint: '#adc6ff'
  primary: '#adc6ff'
  on-primary: '#002e6a'
  primary-container: '#4d8eff'
  on-primary-container: '#00285d'
  inverse-primary: '#005ac2'
  secondary: '#ffb3ad'
  on-secondary: '#68000a'
  secondary-container: '#a40217'
  on-secondary-container: '#ffaea8'
  tertiary: '#4edea3'
  on-tertiary: '#003824'
  tertiary-container: '#00a572'
  on-tertiary-container: '#00311f'
  error: '#ffb4ab'
  on-error: '#690005'
  error-container: '#93000a'
  on-error-container: '#ffdad6'
  primary-fixed: '#d8e2ff'
  primary-fixed-dim: '#adc6ff'
  on-primary-fixed: '#001a42'
  on-primary-fixed-variant: '#004395'
  secondary-fixed: '#ffdad7'
  secondary-fixed-dim: '#ffb3ad'
  on-secondary-fixed: '#410004'
  on-secondary-fixed-variant: '#930013'
  tertiary-fixed: '#6ffbbe'
  tertiary-fixed-dim: '#4edea3'
  on-tertiary-fixed: '#002113'
  on-tertiary-fixed-variant: '#005236'
  background: '#10131a'
  on-background: '#e1e2ec'
  surface-variant: '#32353c'
typography:
  display-lg:
    fontFamily: Geist
    fontSize: 32px
    fontWeight: '700'
    lineHeight: 40px
    letterSpacing: -0.02em
  headline-md:
    fontFamily: Geist
    fontSize: 24px
    fontWeight: '600'
    lineHeight: 32px
    letterSpacing: -0.01em
  headline-sm:
    fontFamily: Geist
    fontSize: 20px
    fontWeight: '600'
    lineHeight: 28px
  body-lg:
    fontFamily: Geist
    fontSize: 16px
    fontWeight: '400'
    lineHeight: 24px
  body-md:
    fontFamily: Geist
    fontSize: 14px
    fontWeight: '400'
    lineHeight: 20px
  body-sm:
    fontFamily: Geist
    fontSize: 13px
    fontWeight: '400'
    lineHeight: 18px
  label-md:
    fontFamily: JetBrains Mono
    fontSize: 12px
    fontWeight: '500'
    lineHeight: 16px
    letterSpacing: 0.02em
  label-sm:
    fontFamily: JetBrains Mono
    fontSize: 10px
    fontWeight: '500'
    lineHeight: 14px
    letterSpacing: 0.05em
rounded:
  sm: 0.25rem
  DEFAULT: 0.5rem
  md: 0.75rem
  lg: 1rem
  xl: 1.5rem
  full: 9999px
spacing:
  unit: 4px
  container-padding: 24px
  gutter: 16px
  compact-gap: 8px
  stack-gap: 12px
---

## Brand & Style

The design system is engineered for high-density IT asset management, prioritizing clarity, technical precision, and rapid data recognition. The brand personality is professional, resilient, and authoritative, reflecting the critical nature of infrastructure monitoring.

The visual style follows a **Corporate / Modern** aesthetic with a lean toward **High-Tech Minimalism**. It utilizes deep tonal layering to separate hardware categories and status levels, ensuring that even with high data density, the interface remains legible and reduces cognitive load for IT administrators. The emotional response is one of control and reliability.

## Colors

This design system utilizes a structured dark-mode palette to ensure high contrast for status-critical information.

- **Primary (Electric Blue):** Used for primary actions, active navigation states, and interactive focused elements.
- **Secondary (Vibrant Red):** Specifically reserved for alerts, critical breakdowns, and destructive actions.
- **Success (Emerald Green):** Indicates "En service" (In Service) status and healthy system pings.
- **Warning (Amber Yellow):** Indicates "En maintenance" (In Maintenance) or pending updates.
- **Neutral/Surface:** A tiered system of slates (`#0F172A` for backgrounds, `#1E293B` for cards/modals) to create a clear visual hierarchy without the harshness of pure black.

## Typography

The typography is optimized for technical data. **Geist** provides a clean, neutral sans-serif foundation for navigation and general interface text. **JetBrains Mono** is employed for labels, serial numbers, IP addresses, and status chips to provide a distinct "developer-tool" feel that aids in differentiating alphanumeric strings.

For mobile viewports, `display-lg` should be capped at `24px` to maintain screen real estate for data tables. High data density is maintained by keeping `body-md` (14px) as the standard text size for most descriptions and list items.

## Layout & Spacing

This design system employs a **Fluid Grid** with a tight spacing rhythm based on a 4px baseline. 

- **Density:** High. Margins and gutters are kept tight (16px gutters) to allow maximum visibility of asset lists and technical specifications.
- **Structure:** A 12-column grid is used for desktop dashboards. On mobile, the layout collapses to a single column with horizontal scrolling for data tables.
- **Alignment:** All technical data (IPs, MAC addresses) should be left-aligned for scanability, while status indicators are centered within compact chips.

## Elevation & Depth

Hierarchy is established through **Tonal Layers** and **Low-Contrast Outlines**.

- **Level 0 (Background):** `#0F172A` - The base canvas.
- **Level 1 (Surface/Cards):** `#1E293B` - Used for primary content containers. These should have a subtle 1px border of `#334155`.
- **Level 2 (Modals/Popovers):** `#1E293B` but with a soft 15% opacity primary-colored shadow (`#3B82F6`) to indicate focus.
- **Separators:** 1px solid lines using `#334155`. Avoid using shadows for separation; rely on border-stroke and background shifts.

## Shapes

The shape language is strictly geometric and "crisp." 

- **General Elements:** 8px (`0.5rem`) corner radius for cards, buttons, and input fields.
- **Status Chips:** 4px (`0.25rem`) for a more technical, utilitarian appearance.
- **Icons:** Use 24px bounding boxes with a 2px stroke weight to match the precision of the typography.

## Components

- **Buttons:** Primary buttons use a solid `#3B82F6` fill with white text. Secondary buttons use an outline style with the `#334155` border. Destructive buttons use a solid `#EF4444` fill.
- **Status Chips:** Small, rectangular badges with a subtle 10% opacity background of their respective status color (Emerald for In Service, Amber for Maintenance) and a solid 1px border.
- **Input Fields:** Background should be slightly darker or same as the surface (`#1E293B`), with a `#334155` border that transitions to `#3B82F6` on focus. Use JetBrains Mono for the input text.
- **Data Tables:** High-density rows (32px to 40px height). Every second row should have a subtle background shift to `#1E293B` (if background) or a slightly lighter slate to assist horizontal tracking.
- **Cards:** Used for individual asset summaries. Cards must include a header section with the asset's primary status indicator in the top-right corner.
- **Asset Indicators:** Use a small colored dot (Success/Warning/Secondary) next to asset names in lists for rapid status checks.