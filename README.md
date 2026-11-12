# LensLite

Tiny Windows 11 tray utility: **Ctrl+Alt+O → drag a region → text is OCR'd offline → Ctrl+V.**
Persian + English, fully in memory (no screenshot ever touches disk), no network, no accounts.

## Status (read this first)
- The source is complete, but **it has not been compiled or run yet.** It was written in a Linux sandbox with no .NET SDK and no internet access, so there is no `.exe` and **no Persian recognition test has been done.** Do not treat Persian support as verified until you run the checklist in `tests/README.md`.
- The `TesseractOCR` API calls in `Ocr.cs` were written from memory of version 5.5.2; if the compiler complains, `Ocr.cs` is the only file likely to need a tweak.

## Engine choice
Tesseract 5 (LSTM) with the official `fas` + `eng` models (Apache-2.0). It is the only one of the three candidates with a Persian-specific model; PaddleOCR/RapidOCR only offer a shared "arabic" recognizer, and Windows' built-in OCR has no Persian pack. Full comparison: `../reports/Persian English OCR engines.md`.
Known risks from the research: Persian digits are a long-standing weak spot, ZWNJ (half-space) is unverified, and nobody publishes screen-text benchmarks, so the bake-off in `tests/README.md` decides whether to switch engine.

## Build (on Windows 11)
1. Install the .NET 8 SDK and the Visual C++ x64 runtime.
2. `powershell -File get-models.ps1` (use `-Set tessdata_fast` for smaller/faster models).
3. `dotnet run -c Release`
4. Portable folder: `dotnet publish -c Release -r win-x64 --self-contained true -o publish`
   (Do not enable trimming; WPF/WinForms trimming is unsupported. Single-file with native DLLs is untested.)

## Use
Tray icon → right-click: Capture, Settings, Exit. Esc or right-click cancels a selection.
Settings: shortcut, languages (fa+en / fa / en), auto-copy, start with Windows (stored in `%AppData%\LensLite\settings.json`).

## Design notes
- `Hotkey.cs` RegisterHotKey on a hidden window; unregistered on exit; reports conflicts.
- `Overlay.cs` captures the whole virtual desktop once, shows it dimmed, you drag a box. The app is PerMonitorV2 DPI aware so coordinates are physical pixels on every monitor.
- `Ocr.cs` upscales small selections (2–3×), adds a white margin, runs Tesseract, normalises Arabic ي/ك to Persian ی/ک.
- Clipboard is only touched when text is non-empty, so failures keep your old clipboard.
- Single instance via a mutex.

## Models and licenses
Tesseract engine and `fas`/`eng` traineddata: Apache-2.0. `TesseractOCR` wrapper: Apache-2.0.
