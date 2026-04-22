# ScreenshotVB — Screenshot Tool for Windows 10

![Platform](https://img.shields.io/badge/platform-Windows%2010-blue)
![Language](https://img.shields.io/badge/language-VB.NET-purple)
![Framework](https://img.shields.io/badge/.NET-10.0-blueviolet)
![License](https://img.shields.io/badge/license-MIT-green)
![Release](https://img.shields.io/github/v/release/johndoe2x/ScreenshotVB)
![Downloads](https://img.shields.io/github/downloads/johndoe2x/ScreenshotVB/total)

A fast, lightweight **screen capture tool for Windows 10** — inspired by [Shottr](https://shottr.cc/) on Mac. Press a hotkey, drag to select a region, then instantly copy, save, or drag the screenshot into any app like WhatsApp, Discord, or Slack.

> **No installation needed. Single `.exe` file. Under 400KB.**

---

## Why ScreenshotVB?

| Feature | ScreenshotVB | Windows Snipping Tool | ShareX |
|---|---|---|---|
| Single exe, no install | ✅ | ❌ | ❌ |
| Global hotkey | ✅ | ✅ | ✅ |
| Drag & drop to any app | ✅ | ❌ | ❌ |
| Lightweight (< 400KB) | ✅ | ✅ | ❌ |
| No account / no cloud | ✅ | ✅ | ✅ |

---

## Features

- **Ctrl+E** — trigger a screenshot from anywhere, anytime
- **Region select** — click and drag to capture any part of your screen
- **Copy** — copies image to clipboard (paste into Paint, Word, Discord, etc.)
- **Save** — save as PNG/BMP/JPEG to any folder
- **Drag & Drop** — drag the screenshot directly into WhatsApp, Discord, Slack, or any folder
- **Open Folder** — opens the auto-save folder in Explorer
- **Auto-save** — every screenshot is saved automatically to `%LocalAppData%\Temp\ScreenshotApp\`

---

## Download

**[Download ScreenshotVB.exe from Releases](https://github.com/johndoe2x/ScreenshotVB/releases/latest)**

### Requirements
- Windows 10 (x64)
- [.NET 10 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) — free, from Microsoft

---

## How to Use

### Taking a Screenshot
1. Run `ScreenshotVB.exe` — a tray icon appears in the bottom-right system tray
2. Press **Ctrl+E** from anywhere
3. Screen dims — click and drag to select the region you want
4. Release — the preview window opens instantly

### Preview Window

| Button | What it does |
|--------|-------------|
| **Copy** | Copies image to clipboard — paste with Ctrl+V anywhere |
| **Save** | Save dialog — choose folder and format (PNG/BMP/JPEG) |
| **Drag & Drop** | Drag into WhatsApp, Discord, Slack, email, or any folder |
| **Open Folder** | Opens the auto-save temp folder in Explorer |

### Cancel
- Press **Esc** during region select to cancel without capturing

---

## Auto-save Location

Every screenshot is automatically saved to:
```
C:\Users\<YourName>\AppData\Local\Temp\ScreenshotApp\
```

---

## Building from Source

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- Visual Studio 2022 with VB.NET / WinForms workload

### Clone & Build
```bash
git clone https://github.com/johndoe2x/ScreenshotVB.git
cd ScreenshotVB
dotnet build
```

### Publish as single exe
```bash
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o "publish"
```

Output: `publish\ScreenshotVB.exe`

---

## Antivirus Note

Some antivirus software (e.g. Avira, Windows Defender) may flag the exe on first run. This is a **false positive** — the app only uses standard Windows APIs for screen capture and hotkeys. Add the exe to your exclusions list to avoid this.

---

## Tech Stack

- **Language:** Visual Basic .NET
- **Framework:** WinForms on .NET 10
- **Target:** Windows 10 x64
- **Size:** ~375KB (framework-dependent single file)

---

## Credits

- **Author:** [johndoe2x](https://github.com/johndoe2x)
- **Inspired by:** [Shottr](https://shottr.cc/) — Mac screenshot tool
- **Built with assistance from:** [Claude](https://claude.ai) by Anthropic
- **App icon:** [Gnome screenshot Icon](https://icon-icons.com/authors/157-sora-meliae-andrea-soragna) by sora-meliae (Andrea Soragna) on [Icon-Icons.com](https://icon-icons.com/authors/157-sora-meliae-andrea-soragna)

If you use or fork this project, a credit back to the original repo is appreciated:
```
Based on ScreenshotVB by johndoe2x — https://github.com/johndoe2x/ScreenshotVB
```

---

## License

MIT — free to use, modify, and distribute.
