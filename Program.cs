using System.Drawing.Imaging;

namespace LensLite;

static class Program
{
    [STAThread]
    static void Main()
    {
        using var mutex = new Mutex(true, @"Local\LensLite.SingleInstance", out bool first);
        if (!first) return; // already running in the tray

        ApplicationConfiguration.Initialize();
        using var app = new TrayApp();
        Application.Run();
    }
}

sealed class TrayApp : IDisposable
{
    readonly Settings settings = Settings.Load();
    readonly Hotkey hotkey = new();
    readonly Ocr ocr = new();
    readonly NotifyIcon tray = new() { Icon = SystemIcons.Application, Text = "LensLite", Visible = true };
    bool busy;

    public TrayApp()
    {
        var menu = new ContextMenuStrip();
        menu.Items.Add("Capture text", null, (_, _) => Capture());
        menu.Items.Add("Settings…", null, (_, _) => OpenSettings());
        menu.Items.Add("Exit", null, (_, _) => { tray.Visible = false; Application.Exit(); });
        tray.ContextMenuStrip = menu;
        tray.MouseDoubleClick += (_, _) => OpenSettings();

        hotkey.Pressed += Capture;
        if (!hotkey.Register(settings.Hotkey))
            Notify($"Shortcut {settings.Hotkey} is taken by another program. Change it in Settings.", error: true);
    }

    void Notify(string text, bool error = false) =>
        tray.ShowBalloonTip(2500, "LensLite", text, error ? ToolTipIcon.Error : ToolTipIcon.Info);

    void OpenSettings()
    {
        using var f = new SettingsForm(settings, combo =>
        {
            if (hotkey.Register(combo)) return true;
            hotkey.Register(settings.Hotkey); // restore the working one
            return false;
        });
        f.ShowDialog();
    }

    async void Capture()
    {
        if (busy) return;
        busy = true;
        try
        {
            Rectangle? sel;
            Bitmap? crop = null;
            bool tooSmall;
            using (var shot = ScreenGrab.Capture(out var vs))
            using (var overlay = new Overlay(shot, vs))
            {
                overlay.ShowDialog();
                sel = overlay.Selection;
                tooSmall = overlay.TooSmall;
                if (sel is { } r) crop = shot.Clone(r, PixelFormat.Format32bppArgb);
            }
            if (crop == null)
            {
                if (tooSmall) Notify("Selection too small.", error: true);
                return; // cancelled
            }

            string text;
            using (crop)
            {
                try { text = await Task.Run(() => ocr.Recognize(crop, settings.Languages)); }
                catch (Exception ex) { Notify("OCR failed: " + ex.Message, error: true); return; }
            }

            if (text.Length == 0) { Notify("No text found. Clipboard unchanged.", error: true); return; }
            if (!settings.AutoCopy) { Notify("Text recognised (auto-copy is off)."); return; }
            Notify(TryCopy(text) ? $"Copied {text.Length} characters." : "Clipboard is busy; text not copied.", error: false);
        }
        finally { busy = false; }
    }

    /// <summary>The clipboard is often briefly locked by another process, so retry a few times.</summary>
    static bool TryCopy(string text)
    {
        for (int i = 0; i < 5; i++)
        {
            try { Clipboard.SetDataObject(text, true, 3, 50); return true; }
            catch (System.Runtime.InteropServices.ExternalException) { Thread.Sleep(80); }
        }
        return false;
    }

    public void Dispose()
    {
        hotkey.Dispose(); // unregisters the shortcut
        ocr.Dispose();
        tray.Dispose();
    }
}
