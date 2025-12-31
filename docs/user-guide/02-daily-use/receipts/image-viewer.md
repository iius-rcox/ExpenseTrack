# Receipt Image Viewer

Zoom, rotate, and navigate receipt images.

## Overview

The image viewer provides tools to examine receipt images clearly. Zoom into details, rotate for proper orientation, and pan across large images.

## Accessing the Viewer

The viewer opens when you:

- Click a receipt thumbnail
- Open receipt details from the dashboard
- Review a match proposal
- Edit extracted fields

## Viewer Interface

### Main Components

| Area | Function |
|------|----------|
| **Image area** | The receipt image display |
| **Toolbar** | Controls for zoom, rotate, fullscreen |
| **Info panel** | Extracted fields (if visible) |
| **Navigation** | Previous/next receipt arrows |

### Toolbar Controls

![Image viewer controls](../../images/receipts/image-viewer-controls.png)
*Caption: Receipt viewer toolbar with zoom and rotate controls*

| Icon | Function | Shortcut |
|------|----------|----------|
| **+** | Zoom in | `+` or `=` |
| **-** | Zoom out | `-` |
| **Fit** | Fit to window | `0` |
| **100%** | Actual size | `1` |
| **↻** | Rotate right | `R` |
| **↺** | Rotate left | `Shift+R` |
| **⛶** | Fullscreen | `F` |
| **⬇** | Download | `D` |

## Zoom Controls

### Zoom Levels

| Level | Use Case |
|-------|----------|
| **Fit** | See entire receipt |
| **50%** | Overview of large receipts |
| **100%** | Read normal text |
| **200%** | Read small print |
| **400%** | Check fine details |

### Zooming Methods

**Mouse/Trackpad**:
- Scroll wheel to zoom in/out
- Pinch gesture (trackpad)
- Click zoom buttons

**Keyboard**:
- `+` or `=` to zoom in
- `-` to zoom out
- `0` to fit
- `1` for 100%

**Touch**:
- Pinch to zoom
- Double-tap to toggle fit/100%

## Panning (Moving Around)

When zoomed in:

**Mouse**:
- Click and drag to pan
- Cursor changes to hand icon

**Keyboard**:
- Arrow keys to pan
- Hold Shift for faster pan

**Touch**:
- Drag with one finger

## Rotation

### When to Rotate

- Receipt photographed sideways
- Upside-down images
- Correcting camera orientation

### Rotation Methods

**Toolbar**: Click rotate buttons

**Keyboard**:
- `R` rotates 90° clockwise
- `Shift+R` rotates 90° counter-clockwise

**Automatic**: Some receipts auto-rotate based on text orientation

### Saving Rotation

Rotation is saved automatically:

- Rotated view persists
- Applied to exports and reports
- Can be changed again anytime

## Fullscreen Mode

### Entering Fullscreen

- Click fullscreen icon in toolbar
- Press `F` key
- Double-click the image

### Fullscreen Features

- Maximum viewing area
- Controls appear on hover
- All shortcuts still work
- Keyboard navigation enabled

### Exiting Fullscreen

- Press `Esc`
- Press `F` again
- Click exit icon

## Navigation Between Receipts

### In List Context

When viewing from a list:

| Action | Method |
|--------|--------|
| **Next** | `→` or `→` arrow button |
| **Previous** | `←` or `←` arrow button |
| **First** | `Home` key |
| **Last** | `End` key |

### Counter Display

Shows position:
- "3 of 15" indicates current receipt
- Updates as you navigate

## Comparing Receipts

### Side-by-Side View

If available:

1. Open first receipt
2. Click **Compare** or split icon
3. Select second receipt
4. View both simultaneously

### In Match Review

During matching:

- Receipt appears on left
- Transaction details on right
- Compare amounts and vendors

## Image Quality

### Understanding Quality Indicators

| Indicator | Meaning |
|-----------|---------|
| **Sharp** | High quality, easy to read |
| **Blurry** | May affect extraction accuracy |
| **Low light** | Dark areas may obscure text |
| **Partial** | Image may be cropped |

### If Quality is Poor

1. Check original file
2. Re-upload if better copy exists
3. Manually enter details if needed
4. Note quality issues for records

## Downloading Images

### Single Receipt

1. Open in viewer
2. Click download icon or press `D`
3. Saves to your downloads folder
4. Original quality preserved

### Multiple Receipts

1. Select multiple from list
2. Click **Download Selected**
3. Saves as ZIP file

## Accessibility Features

### Keyboard Navigation

Full keyboard control:

- Tab through toolbar controls
- Arrow keys for panning
- All actions have shortcuts

### Screen Reader Support

- Image alt text from extraction
- Control labels announced
- Navigation state communicated

### High Contrast

In high contrast mode:

- Toolbar has clear boundaries
- Focus indicators visible
- Icon buttons have text labels

## Troubleshooting

### Image Won't Load

1. Refresh the page
2. Check internet connection
3. Try different browser
4. Contact support if persists

### Rotation Not Saving

1. Wait for save confirmation
2. Refresh and check
3. Rotate again if needed

### Zoom Stuck

1. Press `0` to reset to fit
2. Refresh the page
3. Clear browser cache

## What's Next

After mastering the viewer:

- [AI Extraction](./ai-extraction.md) - Edit extracted fields
- [Uploading](./uploading.md) - Upload quality tips
- [Keyboard Shortcuts](../keyboard-shortcuts.md) - Full shortcut reference

