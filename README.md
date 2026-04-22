# ScreenshotVB

A lightweight screenshot tool for Windows 10, inspired by Shottr on Mac. Press a hotkey, drag a region, and instantly copy, save, or drag the image into any app.

---

## Features

- **Ctrl+E** — trigger a screenshot from anywhere
- **Region select** — click and drag to capture any part of your screen
- **Copy** — copies the image to clipboard (works with Paint, Word, Discord, etc.)
- **Save** — save to any folder via file dialog
- **Drag & Drop** — drag the screenshot directly into WhatsApp, Discord, Slack, or any folder
- **Open Folder** — opens the temp screenshots folder in Explorer
- Auto-saves every screenshot to `%LocalAppData%\Temp\ScreenshotApp\`

---

## Requirements

- Windows 10 (x64)
- [.NET 10 Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) — download **Windows x64 Desktop Runtime**

---

## Download & Run

1. Download `ScreenshotVB.exe` from [Releases](https://github.com/johndoe2x/ScreenshotVB/releases)
2. Run it — no installer needed
3. A tray icon appears in the system tray (bottom right)
4. Press **Ctrl+E** anywhere to take a screenshot

> **Antivirus note:** Some antivirus software (e.g. Avira) may flag the exe on first run. This is a false positive — add the exe to your exclusions list.

---

## How to Use

### Taking a Screenshot
1. Press **Ctrl+E**
2. Your screen dims — click and drag to select the region you want
3. Release the mouse — the preview window opens

### Preview Window Buttons

| Button | What it does |
|--------|-------------|
| **Copy** | Copies image to clipboard — paste with Ctrl+V anywhere |
| **Save** | Opens a Save dialog to save as PNG/BMP/JPEG |
| **Drag & Drop** | Drag this button into any app (WhatsApp, Discord, Slack, a folder) to send the image |
| **Open Folder** | Opens the auto-save folder in Explorer |

### Cancelling
- Press **Esc** during region select to cancel

---

## Building from Source

### Prerequisites
- [.NET 10 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)
- Visual Studio 2022 (with VB.NET / WinForms workload) or VS Code

### Build
```bash
git clone https://github.com/johndoe2x/ScreenshotVB.git
cd ScreenshotVB
dotnet build
```

### Publish (single exe)
```bash
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -o "publish"
```

Output: `publish\ScreenshotVB.exe`

---

## Auto-save Location

Screenshots are saved automatically to:
```
C:\Users\<YourName>\AppData\Local\Temp\ScreenshotApp\
```

---

## Tech Stack

- Language: Visual Basic .NET
- Framework: WinForms on .NET 10
- Target: Windows 10 x64

---

## Credits

- **Author:** [johndoe2x](https://github.com/johndoe2x)
- **Inspired by:** [Shottr](https://shottr.cc/) — a Mac screenshot tool
- **Built with assistance from:** [Claude](https://claude.ai) by Anthropic

If you use or fork this project, a credit back to the original repo is appreciated:
```
Based on ScreenshotVB by johndoe2x — https://github.com/johndoe2x/ScreenshotVB
```
