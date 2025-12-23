# ExpenseFlow Design System
## Luxury Minimalist (Light) + Dark Cyber (Dark Mode)

**Document Version:** 1.0  
**Last Updated:** December 2025  
**Audience:** AI Design Agents, Developers, Designers

---

## Table of Contents

1. [Overview](#overview)
2. [Theme 1: Luxury Minimalist (Light Mode)](#theme-1-luxury-minimalist-light-mode)
3. [Theme 2: Dark Cyber (Dark Mode)](#theme-2-dark-cyber-dark-mode)
4. [Shared Components](#shared-components)
5. [Implementation Guide](#implementation-guide)

---

## Overview

Create a dual-theme design system for **ExpenseFlow** (expense management dashboard) with two distinct visual modes:

- **Light Mode: Luxury Minimalist** - Premium, elegant, minimal aesthetic
- **Dark Mode: Dark Cyber** - Modern fintech, glassmorphism, neon accents

Users should be able to toggle between light and dark modes via a theme switcher button in the top navigation bar.

**Core Design Principles:**
- Minimalist approach with maximum clarity
- Subtle interactions and smooth transitions
- Consistent spacing and typography
- Accessibility maintained across both themes
- Professional, premium appearance

---

---

# THEME 1: LUXURY MINIMALIST (LIGHT MODE)

## Color Palette
```css
:root {
  /* Light Mode - Luxury Minimalist */
  --light-bg-primary: #fafaf8;        /* Off-white, slightly warm */
  --light-bg-secondary: #f5f5f3;      /* Slightly darker off-white for depth */
  --light-bg-card: #ffffff;            /* Pure white cards and containers */
  --light-accent-primary: #2d5f4f;    /* Deep emerald green - main accent */
  --light-accent-secondary: #4a8f75;  /* Lighter emerald for hover states */
  --light-text-primary: #1a1a1a;      /* Near-black, primary text */
  --light-text-secondary: #666666;    /* Medium gray for body text */
  --light-text-tertiary: #999999;     /* Light gray for labels and descriptions */
  --light-border: #f0f0f0;            /* Very subtle borders */
  --light-border-dark: #e5e5e5;       /* Slightly darker borders for emphasis */
  --light-shadow-light: 0 2px 8px rgba(0, 0, 0, 0.04);
  --light-shadow-medium: 0 8px 24px rgba(45, 95, 79, 0.1);
  --light-shadow-dark: 0 12px 36px rgba(45, 95, 79, 0.15);
}
```

### Color Usage Guide

| Color | Hex | Usage |
|-------|-----|-------|
| Primary Background | #fafaf8 | Page background, sidebar |
| Card Background | #ffffff | Cards, containers, sections |
| Accent (Primary) | #2d5f4f | Buttons, active states, headings, accent elements |
| Text (Primary) | #1a1a1a | Main body text, headings |
| Text (Secondary) | #666666 | Secondary text, descriptions |
| Text (Tertiary) | #999999 | Labels, small text, helpers |
| Border | #f0f0f0 | Card borders, dividers |

---

## Typography

### Font Stack
```css
font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', 'Roboto', 'Oxygen', 'Ubuntu', 'Cantarell', 'Fira Sans', 'Droid Sans', 'Helvetica Neue', sans-serif;
```

### Typography Scale

| Element | Size | Weight | Color | Letter-Spacing | Line-Height | Usage |
|---------|------|--------|-------|-----------------|------------|-------|
| H1 (Page Title) | 32px | 300 | #2d5f4f | -1px | 1.2 | Main page titles |
| H2 (Section Title) | 18px | 600 | #2d5f4f | -0.5px | 1.3 | Section headings |
| H3 (Subsection) | 16px | 500 | #1a1a1a | 0 | 1.3 | Subsection headings |
| Body Text | 14px | 400 | #666 | 0 | 1.6 | Standard text content |
| Small Text/Label | 12px | 500 | #999 | 0.5px | 1.5 | Labels, helpers, small text |
| Stat Value (Large) | 42px | 300 | #2d5f4f | -1px | 1.1 | Large stat numbers |
| Stat Value (Small) | 28px | 300 | #2d5f4f | -0.5px | 1.1 | Medium stat numbers |

### Font Weight Usage

- **300 (Light)** - Large numbers, elegant headings
- **400 (Regular)** - Body text, descriptions
- **500 (Medium)** - Small labels, button text
- **600 (Semi-bold)** - Section titles, emphasis

### CSS Implementation
```css
h1 {
  font-size: 32px;
  font-weight: 300;
  letter-spacing: -1px;
  color: #2d5f4f;
  line-height: 1.2;
}

h2 {
  font-size: 18px;
  font-weight: 600;
  letter-spacing: -0.5px;
  color: #2d5f4f;
  line-height: 1.3;
  margin-bottom: 24px;
}

h3 {
  font-size: 16px;
  font-weight: 500;
  color: #1a1a1a;
  line-height: 1.3;
  margin-bottom: 16px;
}

body {
  font-size: 14px;
  font-weight: 400;
  color: #666;
  line-height: 1.6;
}

.stat-value {
  font-size: 42px;
  font-weight: 300;
  color: #2d5f4f;
  letter-spacing: -1px;
}

.label {
  font-size: 12px;
  font-weight: 500;
  color: #999;
  letter-spacing: 0.5px;
  text-transform: uppercase;
}
```

---

## Sidebar Navigation

### Container
- **Background**: `#fafaf8`
- **Border Right**: `1px solid #f0f0f0`
- **Width**: `240px` (desktop), collapsible on mobile
- **Padding**: `24px 0`
- **Position**: Fixed left side

### Logo/Brand Area
- **Padding**: `20px 24px`
- **Border Bottom**: `1px solid #f0f0f0`
- **Logo Text Color**: `#2d5f4f`
- **Logo Font-Weight**: 300
- **Logo Font-Size**: 18px
- **Subtitle Color**: `#999`
- **Subtitle Font-Size**: 12px

### Menu Items
```css
.sidebar-menu-item {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 16px 20px;
  margin: 0 12px;
  border-left: 3px solid transparent;
  border-radius: 0 4px 4px 0;
  color: #666;
  font-size: 14px;
  font-weight: 400;
  transition: all 0.3s ease;
  cursor: pointer;
}

.sidebar-menu-item:hover {
  color: #2d5f4f;
  background: rgba(45, 95, 79, 0.05);
  border-left-color: rgba(45, 95, 79, 0.3);
}

.sidebar-menu-item.active {
  color: #2d5f4f;
  border-left-color: #2d5f4f;
  background: rgba(45, 95, 79, 0.08);
}

.sidebar-menu-icon {
  width: 20px;
  height: 20px;
  color: currentColor;
}
```

### User Profile Section (Bottom)
- **Padding**: `20px 24px`
- **Border Top**: `1px solid #f0f0f0`
- **Layout**: Flex row with avatar, name, dropdown
- **Avatar**: 40px circle, light gray background `#e5e5e5`
- **Name Color**: `#1a1a1a`
- **Name Font-Weight**: 500
- **Email Color**: `#999`
- **Email Font-Size**: 12px

---

## Top Navigation Bar

### Container
```css
.top-bar {
  background: #ffffff;
  border-bottom: 1px solid #f0f0f0;
  height: 60px;
  padding: 0 32px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.04);
}
```

### Breadcrumb
- **Font-Size**: 12px
- **Color**: `#999`
- **Gap**: `8px`
- **Separator**: `/`

### Right Section (Buttons)
- **Display**: Flex
- **Gap**: `12px`
- **Align**: Center

### Icon Button
```css
.btn-icon {
  width: 40px;
  height: 40px;
  border-radius: 4px;
  border: 1px solid #f0f0f0;
  background: transparent;
  color: #2d5f4f;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.2s ease;
  font-size: 18px;
}

.btn-icon:hover {
  border-color: #2d5f4f;
  background: rgba(45, 95, 79, 0.05);
}

.btn-icon:active {
  background: rgba(45, 95, 79, 0.1);
}
```

---

## Main Content Area

### Layout
```css
.page-container {
  max-width: 1400px;
  margin: 0 auto;
  padding: 40px;
  background: #fafaf8;
  min-height: calc(100vh - 60px);
}
```

### Spacing Between Sections
- **Vertical Gap**: `32px`
- **Horizontal Gap**: `24px` (for multi-column layouts)

---

## Stat Cards (KPI Boxes)

### Container
```css
.stat-card {
  background: #ffffff;
  border: 2px solid #f0f0f0;
  border-radius: 8px;
  padding: 32px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.04);
  transition: all 0.3s ease;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  min-height: 200px;
}

.stat-card:hover {
  border-color: #2d5f4f;
  box-shadow: 0 8px 24px rgba(45, 95, 79, 0.1);
  transform: translateY(-2px);
}
```

### Label
```css
.stat-label {
  font-size: 12px;
  font-weight: 500;
  color: #999;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  margin-bottom: 12px;
}
```

### Value (Large Number)
```css
.stat-value {
  font-size: 42px;
  font-weight: 300;
  color: #2d5f4f;
  letter-spacing: -1px;
  margin-bottom: 8px;
}
```

### Description/Subtitle
```css
.stat-description {
  font-size: 12px;
  color: #999;
  font-weight: 400;
}
```

### Grid Layout
```css
.stats-grid {
  display: grid;
  grid-template-columns: repeat(auto-fit, minmax(280px, 1fr));
  gap: 24px;
  margin-bottom: 32px;
}
```

---

## Section Containers

### Card/Container
```css
.section {
  background: #ffffff;
  border: 1px solid #f0f0f0;
  border-radius: 8px;
  padding: 32px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.04);
  transition: all 0.3s ease;
}

.section:hover {
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.06);
}
```

### Section Title
```css
.section-title {
  font-size: 18px;
  font-weight: 600;
  color: #2d5f4f;
  margin-bottom: 24px;
  letter-spacing: -0.5px;
}
```

### Section Subtitle
```css
.section-subtitle {
  font-size: 14px;
  color: #666;
  margin-bottom: 20px;
  font-weight: 400;
}
```

---

## Empty States
```css
.empty-state {
  text-align: center;
  padding: 48px 24px;
  color: #999;
}

.empty-state-icon {
  font-size: 56px;
  color: #ddd;
  margin-bottom: 16px;
  opacity: 0.6;
}

.empty-state-title {
  font-size: 16px;
  font-weight: 600;
  color: #1a1a1a;
  margin-bottom: 8px;
}

.empty-state-message {
  font-size: 14px;
  color: #999;
  line-height: 1.6;
}
```

---

## Buttons

### Primary Button (CTA)
```css
.btn-primary {
  background: #2d5f4f;
  color: #ffffff;
  padding: 12px 24px;
  border: none;
  border-radius: 4px;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;
  display: inline-flex;
  align-items: center;
  gap: 8px;
}

.btn-primary:hover {
  background: #204a3a;
  box-shadow: 0 4px 12px rgba(32, 74, 58, 0.15);
}

.btn-primary:active {
  background: #1a3a2e;
  box-shadow: inset 0 2px 4px rgba(0, 0, 0, 0.1);
}

.btn-primary:disabled {
  background: #ccc;
  cursor: not-allowed;
  box-shadow: none;
}
```

### Secondary Button
```css
.btn-secondary {
  background: transparent;
  color: #2d5f4f;
  border: 1px solid #2d5f4f;
  padding: 12px 24px;
  border-radius: 4px;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;
  display: inline-flex;
  align-items: center;
  gap: 8px;
}

.btn-secondary:hover {
  background: rgba(45, 95, 79, 0.1);
  border-color: #2d5f4f;
}

.btn-secondary:active {
  background: rgba(45, 95, 79, 0.2);
}
```

### Ghost Button (Tertiary)
```css
.btn-ghost {
  background: transparent;
  color: #666;
  border: 1px solid #ddd;
  padding: 12px 24px;
  border-radius: 4px;
  font-size: 14px;
  font-weight: 500;
  cursor: pointer;
  transition: all 0.2s ease;
}

.btn-ghost:hover {
  border-color: #2d5f4f;
  color: #2d5f4f;
  background: rgba(45, 95, 79, 0.05);
}
```

---

## Forms & Inputs

### Text Input
```css
input[type="text"],
input[type="email"],
input[type="number"],
input[type="search"],
textarea {
  width: 100%;
  background: #ffffff;
  border: 1px solid #f0f0f0;
  border-radius: 4px;
  padding: 10px 12px;
  font-size: 14px;
  color: #1a1a1a;
  font-family: inherit;
  transition: all 0.2s ease;
}

input[type="text"]:hover,
textarea:hover {
  border-color: #e5e5e5;
}

input[type="text"]:focus,
textarea:focus {
  outline: none;
  border-color: #2d5f4f;
  box-shadow: 0 0 0 3px rgba(45, 95, 79, 0.1);
}

input::placeholder {
  color: #ccc;
}
```

### Select/Dropdown
```css
select {
  width: 100%;
  background: #ffffff url('data:image/svg+xml;charset=utf-8,<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 24 24" fill="%232d5f4f"><path d="M7 10l5 5 5-5z"/></svg>') no-repeat right 12px center;
  background-size: 20px;
  border: 1px solid #f0f0f0;
  border-radius: 4px;
  padding: 10px 12px 10px 12px;
  padding-right: 36px;
  font-size: 14px;
  color: #1a1a1a;
  appearance: none;
  cursor: pointer;
  transition: all 0.2s ease;
}

select:hover {
  border-color: #e5e5e5;
}

select:focus {
  outline: none;
  border-color: #2d5f4f;
  box-shadow: 0 0 0 3px rgba(45, 95, 79, 0.1);
}
```

### Form Group
```css
.form-group {
  margin-bottom: 20px;
}

.form-label {
  display: block;
  font-size: 12px;
  font-weight: 600;
  color: #1a1a1a;
  margin-bottom: 8px;
  text-transform: uppercase;
  letter-spacing: 0.5px;
}

.form-helper {
  font-size: 12px;
  color: #999;
  margin-top: 4px;
}

.form-error {
  color: #d32f2f;
  font-size: 12px;
  margin-top: 4px;
}

input.error,
textarea.error {
  border-color: #d32f2f;
  box-shadow: 0 0 0 3px rgba(211, 47, 47, 0.1);
}
```

---

## Badges & Tags

### Badge
```css
.badge {
  display: inline-flex;
  align-items: center;
  gap: 6px;
  background: rgba(45, 95, 79, 0.1);
  color: #2d5f4f;
  padding: 4px 8px;
  border-radius: 4px;
  font-size: 11px;
  font-weight: 600;
  letter-spacing: 0.3px;
}

.badge.success {
  background: rgba(76, 175, 80, 0.1);
  color: #4caf50;
}

.badge.warning {
  background: rgba(255, 152, 0, 0.1);
  color: #ff9800;
}

.badge.danger {
  background: rgba(211, 47, 47, 0.1);
  color: #d32f2f;
}

.badge.info {
  background: rgba(33, 150, 243, 0.1);
  color: #2196f3;
}
```

---

## Data Visualizations (Charts)

### Chart Container
```css
.chart-container {
  background: #ffffff;
  border: 1px solid #f0f0f0;
  border-radius: 8px;
  padding: 24px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.04);
}

.chart-title {
  font-size: 16px;
  font-weight: 600;
  color: #1a1a1a;
  margin-bottom: 20px;
}
```

### Chart Colors & Elements
```css
/* SVG/Chart Elements */
.chart-grid {
  stroke: #f0f0f0;
  stroke-dasharray: 2, 2;
  opacity: 0.5;
}

.chart-axis-line {
  stroke: #f0f0f0;
}

.chart-axis-text {
  fill: #999;
  font-size: 12px;
}

.chart-bar,
.chart-line {
  fill: #2d5f4f;
  stroke: #2d5f4f;
  transition: all 0.2s ease;
}

.chart-bar:hover {
  fill: #4a8f75;
  filter: brightness(1.1);
}

.chart-legend {
  font-size: 12px;
  color: #666;
}

.chart-legend-item {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-right: 16px;
}

.chart-legend-color {
  width: 12px;
  height: 12px;
  border-radius: 2px;
  background: #2d5f4f;
}
```

---

## Tables
```css
table {
  width: 100%;
  border-collapse: collapse;
  background: #ffffff;
}

thead {
  background: #f5f5f3;
  border-bottom: 1px solid #f0f0f0;
}

th {
  padding: 12px 16px;
  text-align: left;
  font-size: 12px;
  font-weight: 600;
  color: #1a1a1a;
  letter-spacing: 0.5px;
  text-transform: uppercase;
}

tbody tr {
  border-bottom: 1px solid #f0f0f0;
  transition: background-color 0.2s ease;
}

tbody tr:hover {
  background-color: #fafaf8;
}

td {
  padding: 12px 16px;
  font-size: 14px;
  color: #1a1a1a;
}

td.muted {
  color: #999;
}
```

---

## Dividers & Spacing

### Divider
```css
.divider {
  border: none;
  border-top: 1px solid #f0f0f0;
  margin: 24px 0;
}

.divider.compact {
  margin: 12px 0;
}
```

### Spacing Scale

Use these values for margin and padding:

- **4px** - `xs` (micro spacing)
- **8px** - `sm` (small)
- **12px** - `md` (medium)
- **16px** - `lg` (standard)
- **20px** - `xl` (comfortable)
- **24px** - `2xl` (spacing between sections)
- **32px** - `3xl` (large spacing)
- **40px** - `4xl` (page padding)
- **64px** - `5xl` (major separation)

---

---

# THEME 2: DARK CYBER (DARK MODE)

## Color Palette
```css
:root {
  /* Dark Mode - Dark Cyber */
  --dark-bg-primary: #0f1419;         /* Very dark navy-black */
  --dark-bg-secondary: #1a1f2e;       /* Dark navy with slight blue tone */
  --dark-bg-card: #1e2438;            /* Card background, slightly lighter */
  --dark-accent-primary: #00bcd4;     /* Bright cyan - main accent */
  --dark-accent-secondary: #1e88e5;   /* Electric blue - secondary accent */
  --dark-accent-tertiary: #7b1fa2;    /* Purple - accent color 3 */
  --dark-text-primary: #e0e0e0;       /* Light gray for main text */
  --dark-text-secondary: #b0b0b0;     /* Medium gray for secondary text */
  --dark-text-tertiary: #808080;      /* Dark gray for labels */
  --dark-border: rgba(255, 255, 255, 0.1);      /* Subtle light borders */
  --dark-border-accent: rgba(0, 188, 212, 0.3); /* Cyan-tinted borders */
  --dark-shadow-light: 0 8px 32px rgba(0, 0, 0, 0.3);
  --dark-shadow-medium: 0 12px 48px rgba(0, 188, 212, 0.2);
  --dark-shadow-dark: 0 16px 64px rgba(0, 0, 0, 0.5);
  --dark-glassmorphism-bg: rgba(255, 255, 255, 0.08);
  --dark-glassmorphism-border: rgba(255, 255, 255, 0.1);
}
```

### Color Usage Guide

| Color | Hex/Value | Usage |
|-------|-----------|-------|
| Primary Background | #0f1419 | Page background |
| Secondary Background | #1a1f2e | Content areas |
| Card Background | #1e2438 | Cards, containers |
| Accent (Primary) | #00bcd4 | Buttons, active states, headings |
| Accent (Secondary) | #1e88e5 | Gradients, secondary elements |
| Text (Primary) | #e0e0e0 | Main body text |
| Text (Secondary) | #b0b0b0 | Secondary text |
| Text (Tertiary) | #808080 | Labels, small text |
| Border | rgba(255,255,255,0.1) | Card borders, dividers |
| Glassmorphism Background | rgba(255,255,255,0.08) | Frosted glass effect |

---

## Typography (Dark Mode)

### Typography Scale

| Element | Size | Weight | Color | Letter-Spacing | Notes |
|---------|------|--------|-------|-----------------|-------|
| H1 | 32px | 300 | Gradient: #00bcd4→#1e88e5 | -1px | Cyan gradient effect |
| H2 | 18px | 600 | #00bcd4 | -0.5px | - |
| H3 | 16px | 500 | #e0e0e0 | 0 | - |
| Body Text | 14px | 400 | #b0b0b0 | 0 | Lighter for dark backgrounds |
| Small Text/Label | 12px | 500 | #808080 | 0.5px | - |
| Stat Value (Large) | 42px | 300 | Gradient: #00bcd4→#1e88e5 | -1px | Gradient text |
| Stat Value (Small) | 28px | 300 | #00bcd4 | -0.5px | - |

### CSS Implementation for Dark Mode
```css
h1 {
  font-size: 32px;
  font-weight: 300;
  letter-spacing: -1px;
  background: linear-gradient(90deg, #00bcd4, #1e88e5);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  line-height: 1.2;
}

h2 {
  font-size: 18px;
  font-weight: 600;
  letter-spacing: -0.5px;
  color: #00bcd4;
  line-height: 1.3;
  margin-bottom: 24px;
}

h3 {
  font-size: 16px;
  font-weight: 500;
  color: #e0e0e0;
  line-height: 1.3;
  margin-bottom: 16px;
}

body {
  font-size: 14px;
  font-weight: 400;
  color: #b0b0b0;
  line-height: 1.6;
}

.stat-value {
  font-size: 42px;
  font-weight: 300;
  background: linear-gradient(135deg, #00bcd4 0%, #1e88e5 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  letter-spacing: -1px;
}

.label {
  font-size: 12px;
  font-weight: 500;
  color: #808080;
  letter-spacing: 0.5px;
  text-transform: uppercase;
}
```

---

## Sidebar Navigation (Dark Mode)

### Container
- **Background**: `#0f1419`
- **Border Right**: `1px solid rgba(255, 255, 255, 0.1)`
- **Width**: `240px`
- **Padding**: `24px 0`

### Logo/Brand Area
- **Padding**: `20px 24px`
- **Border Bottom**: `1px solid rgba(255, 255, 255, 0.1)`
- **Logo Text Color**: `#00bcd4`
- **Logo Font-Weight**: 300
- **Subtitle Color**: `#808080`

### Menu Items
```css
.sidebar-menu-item {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 16px 20px;
  margin: 0 12px;
  border-left: 3px solid transparent;
  border-radius: 0 4px 4px 0;
  color: #b0b0b0;
  font-size: 14px;
  font-weight: 400;
  transition: all 0.3s ease;
  cursor: pointer;
}

.sidebar-menu-item:hover {
  color: #00bcd4;
  border-left-color: rgba(0, 188, 212, 0.3);
  background: rgba(0, 188, 212, 0.08);
}

.sidebar-menu-item.active {
  color: #00bcd4;
  border-left-color: #00bcd4;
  background: rgba(0, 188, 212, 0.12);
  box-shadow: inset -4px 0 0 rgba(0, 188, 212, 0.2);
}
```

---

## Top Navigation Bar (Dark Mode)

### Container
```css
.top-bar {
  background: #1a1f2e;
  border-bottom: 1px solid rgba(255, 255, 255, 0.1);
  height: 60px;
  padding: 0 32px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.3);
}
```

### Breadcrumb
- **Color**: `#808080`
- **Font-Size**: 12px

### Icon Button (Dark Mode)
```css
.btn-icon {
  width: 40px;
  height: 40px;
  border-radius: 4px;
  border: 1px solid rgba(255, 255, 255, 0.1);
  background: rgba(255, 255, 255, 0.05);
  color: #00bcd4;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.2s ease;
  font-size: 18px;
}

.btn-icon:hover {
  border-color: rgba(0, 188, 212, 0.3);
  background: rgba(0, 188, 212, 0.1);
  box-shadow: 0 0 20px rgba(0, 188, 212, 0.2);
}
```

---

## Main Content Area (Dark Mode)

### Layout
```css
.page-container {
  max-width: 1400px;
  margin: 0 auto;
  padding: 40px;
  background: linear-gradient(135deg, #0f1419 0%, #1a1f2e 100%);
  min-height: calc(100vh - 60px);
}
```

---

## Stat Cards (Dark Mode - Glassmorphic)

### Container
```css
.stat-card {
  background: rgba(255, 255, 255, 0.08);
  backdrop-filter: blur(10px);
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 16px;
  padding: 24px;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.3);
  transition: all 0.3s ease;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  min-height: 180px;
  position: relative;
  overflow: hidden;
}

.stat-card::before {
  content: '';
  position: absolute;
  top: 0;
  left: -100%;
  width: 100%;
  height: 100%;
  background: linear-gradient(90deg, transparent, rgba(0, 188, 212, 0.1), transparent);
  transition: left 0.6s ease;
  pointer-events: none;
}

.stat-card:hover {
  border-color: rgba(0, 188, 212, 0.3);
  background: rgba(255, 255, 255, 0.12);
  box-shadow: 0 12px 48px rgba(0, 188, 212, 0.2);
  transform: translateY(-4px);
}

.stat-card:hover::before {
  left: 100%;
}
```

### Label (Dark Mode)
```css
.stat-label {
  font-size: 12px;
  font-weight: 500;
  color: #808080;
  text-transform: uppercase;
  letter-spacing: 0.5px;
  margin-bottom: 12px;
}
```

### Value (Dark Mode with Gradient)
```css
.stat-value {
  font-size: 42px;
  font-weight: 300;
  background: linear-gradient(135deg, #00bcd4 0%, #1e88e5 100%);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  letter-spacing: -1px;
  margin-bottom: 8px;
}
```

### Description (Dark Mode)
```css
.stat-description {
  font-size: 12px;
  color: #00bcd4;
  font-weight: 400;
  opacity: 0.8;
}
```

---

## Section Containers (Dark Mode)

### Card/Container (Glassmorphic)
```css
.section {
  background: rgba(255, 255, 255, 0.08);
  backdrop-filter: blur(10px);
  border: 1px solid rgba(255, 255, 255, 0.1);
  border-radius: 12px;
  padding: 32px;
  box-shadow: 0 8px 32px rgba(0, 0, 0, 0.3);
  transition: all 0.3s ease;
}

.section:hover {
  border-color: rgba(0, 188, 212, 0.2);
  box-shadow: 0 12px 48px rgba(0, 188, 212, 0.15);
}
```

### Section Title (Dark Mode)
```css
.section-title {
  font-size: 18px;
  font-weight: 600;
  color: #00bcd4;
  margin-bottom: 24px;
  letter-spacing: -0.5px;
}
```

---

## Empty States (Dark Mode)
```css
.empty-state {
  text-align: center;
  padding: 48px 24px;
  color: #808080;
}

.empty-state-icon {
  font-size: 56px;
  color: rgba(0, 188, 212, 0.3);
  margin-bottom: 16px;
  opacity: 0.5;
}

.empty-state-title {
  font-size: 16px;
  font-weight: 600;
  color: #e0e0e0;
  margin-bottom: 8px;
}

.empty-state-message {
  font-size: 14px;
  color: #b0b0b0;
  line-height: 1.6;
}
```

---

## Buttons (Dark Mode)

### Primary Button (Dark Mode)
```css
.btn-primary {
  background: linear-gradient(135deg, #00bcd4, #1e88e5);
  color: #0f1419;
  padding: 12px 24px;
  border: none;
  border-radius: 4px;
  font-size: 14px;
  font-weight: 600;
  cursor: pointer;
  transition: all 0.2s ease;
  display: inline-flex;
  align-items: center;
  gap: 8px;
  box-shadow: 0 4px 12px rgba(0, 188, 212, 0.2);
}

.btn-primary:hover {
  box-shadow: 0 8px 24px rgba(0, 188, 212,