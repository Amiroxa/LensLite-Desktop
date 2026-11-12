using System.Runtime.InteropServices;

namespace LensLite;

/// <summary>System-wide hotkey via RegisterHotKey on a hidden message-only window.</summary>
sealed class Hotkey : NativeWindow, IDisposable
{
    [DllImport("user32.dll", SetLastError = true)] static extern bool RegisterHotKey(IntPtr hWnd, int id, uint mods, uint vk);
    [DllImport("user32.dll")] static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    const int WM_HOTKEY = 0x0312, Id = 1;
    const uint MOD_ALT = 1, MOD_CONTROL = 2, MOD_SHIFT = 4, MOD_WIN = 8, MOD_NOREPEAT = 0x4000;

    public event Action? Pressed;

    public Hotkey() => CreateHandle(new CreateParams());

    /// <summary>Replaces the current registration. Returns false if the combo is invalid or taken by another app.</summary>
    public bool Register(string combo)
    {
        UnregisterHotKey(Handle, Id);
        return TryParse(combo, out var mods, out var vk) && RegisterHotKey(Handle, Id, mods | MOD_NOREPEAT, vk);
    }

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY) Pressed?.Invoke();
        base.WndProc(ref m);
    }

    public void Dispose()
    {
        UnregisterHotKey(Handle, Id);
        DestroyHandle();
    }

    /// <summary>Parses "Ctrl+Alt+O". At least one modifier is required so plain typing is never hijacked.</summary>
    public static bool TryParse(string combo, out uint mods, out uint vk)
    {
        mods = 0; vk = 0;
        foreach (var part in combo.Split('+', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            switch (part.ToLowerInvariant())
            {
                case "ctrl": mods |= MOD_CONTROL; break;
                case "alt": mods |= MOD_ALT; break;
                case "shift": mods |= MOD_SHIFT; break;
                case "win": mods |= MOD_WIN; break;
                default:
                    if (vk != 0 || !Enum.TryParse<Keys>(part, true, out var k) || k is Keys.None) return false;
                    vk = (uint)k;
                    break;
            }
        }
        return mods != 0 && vk != 0;
    }

    public static string Format(Keys modifiers, Keys key)
    {
        var parts = new List<string>();
        if (modifiers.HasFlag(Keys.Control)) parts.Add("Ctrl");
        if (modifiers.HasFlag(Keys.Alt)) parts.Add("Alt");
        if (modifiers.HasFlag(Keys.Shift)) parts.Add("Shift");
        parts.Add(key.ToString());
        return string.Join("+", parts);
    }
}
