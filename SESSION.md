# Session Log

## 2026-04-22

### What was done

- Resumed project after Warp terminal froze
- Confirmed V1 fully working (all source files intact, exe published)
- Uploaded project to GitHub for the first time
  - Repo: https://github.com/johndoe2x/ScreenshotVB
  - Set up git identity (email: jrsolutionsceo@gmail.com, username: johndoe2x)
  - Created `.gitignore` (excludes bin/, obj/, publish/, .vs/)
  - Initial commit: 7 files (MainForm.vb, PreviewForm.vb, SelectorForm.vb, Program.vb, ScreenshotVB.vbproj, app.ico, .gitignore)
- Created `README.md` with full usage instructions, requirements, build steps, antivirus note
- Added Credits section to README (author, Shottr inspiration, Claude assist, fork attribution line)

### Current State

- GitHub repo is live and up to date
- V1 feature set complete and working:
  - Ctrl+E hotkey
  - Region select overlay
  - Copy / Save / Drag & Drop / Open Folder
  - Auto-save to %LocalAppData%\Temp\ScreenshotApp\
  - Clipboard works with Paint (BMP) and Ctrl+V into folders (FileDrop)

### Next Steps (not started)

- Add a GitHub Release with the compiled exe attached
- Potential V2 features: annotations (arrow, text, blur), pin-to-screen

---

## 2026-04-22 (Session 2)

### What was done

- **Fixed: text labels are now moveable**
  - Text was previously baked into the annotation layer as pixels — impossible to reposition
  - Refactored: text is now stored as `TextLabel` objects (like `ArrowShape` for arrows)
  - Drawn dynamically in `Canvas_Paint` via `DrawLabelOnCanvas`
  - Move tool now works on text — hover to get crosshair cursor, click & drag to reposition
  - `HitTestLabel` added for click detection using `GraphicsPath.GetBounds()`
  - Baked correctly into merged output on Copy/Save via `GetMergedBitmap`
  - Undo removes last text label first, then arrows, then pen strokes
  - Removed dead `DrawTextOnLayer` method

- **Fixed: stuck dark input boxes**
  - Clicking the Text tool multiple times rapidly left orphaned TextBox controls on the canvas
  - Added `_activeTextBox` field — `ShowTextInput` now dismisses any open box before creating a new one
  - `RemoveTb` clears `_activeTextBox` on cleanup

- **README updated**
  - Added `assets/preview.png` — app screenshot shown centered below intro
  - Updated Features list to include all V2 tools (Pen, Arrow, Text, Move, Eraser, Undo, Pin)
  - Updated Preview Window table with all toolbar buttons

- **GitHub release updated**
  - Latest exe uploaded to v2.0.0 release (`--clobber`)

### Commits this session

- `77978e4` — Make text labels moveable via Move tool
- `7d92aad` — Add app preview screenshot and update README for V2

### Current State

- GitHub repo: https://github.com/johndoe2x/ScreenshotVB
- Latest release: v2.0.0 (exe updated 2026-04-22)
- All V2 features working:
  - Pen, Arrow, Text (moveable), Move, Eraser, Undo, Pin
  - Copy/Save bakes all annotations at full resolution
  - No stuck input boxes

### Next Steps

- V2 remaining roadmap (TODOLIST.md):
  - Blur / redact tool
  - Delay capture (countdown)
  - Window snap (click a window to capture it)
  - Recent screenshots in tray menu
