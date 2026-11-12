using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace LensLite;

static class ScreenGrab
{
    /// <summary>Grabs the whole virtual desktop (all monitors) into memory, in physical pixels.</summary>
    public static Bitmap Capture(out Rectangle virtualScreen)
    {
        virtualScreen = SystemInformation.VirtualScreen;
        var bmp = new Bitmap(virtualScreen.Width, virtualScreen.Height, PixelFormat.Format32bppArgb);
        using var g = Graphics.FromImage(bmp);
        g.CopyFromScreen(virtualScreen.X, virtualScreen.Y, 0, 0, virtualScreen.Size, CopyPixelOperation.SourceCopy);
        return bmp;
    }
}

/// <summary>
/// Full-desktop topmost window showing the frozen screenshot slightly dimmed.
/// Drag to select; Esc or right-click cancels. Selection is in screenshot pixel coordinates.
/// </summary>
sealed class Overlay : Form
{
    [DllImport("user32.dll")] static extern bool SetWindowPos(IntPtr h, IntPtr after, int x, int y, int w, int hgt, uint flags);
    const int WM_DPICHANGED = 0x02E0;

    readonly Bitmap shot;
    readonly Rectangle vs;
    Point start, cur;
    bool drag;

    public Rectangle? Selection { get; private set; }
    public bool TooSmall { get; private set; }

    public Overlay(Bitmap shot, Rectangle virtualScreen)
    {
        this.shot = shot;
        vs = virtualScreen;
        AutoScaleMode = AutoScaleMode.None;
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.Manual;
        ShowInTaskbar = false;
        TopMost = true;
        DoubleBuffered = true;
        Cursor = Cursors.Cross;
        Bounds = vs;
    }

    // A window spanning monitors with different DPI would otherwise be rescaled by WinForms.
    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_DPICHANGED) return;
        base.WndProc(ref m);
    }

    protected override void OnShown(EventArgs e)
    {
        base.OnShown(e);
        SetWindowPos(Handle, new IntPtr(-1), vs.X, vs.Y, vs.Width, vs.Height, 0x0040 /* SHOWWINDOW */);
        Activate();
    }

    static Rectangle Normalize(Point a, Point b) =>
        new(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button != MouseButtons.Left) { Close(); return; }
        start = cur = e.Location;
        drag = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (!drag) return;
        var old = Normalize(start, cur);
        cur = e.Location;
        // Repaint only the changed area; redrawing a multi-monitor bitmap on every move is slow.
        Invalidate(Rectangle.Inflate(Rectangle.Union(old, Normalize(start, cur)), 6, 32));
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (!drag) return;
        drag = false;
        var r = Rectangle.Intersect(Normalize(start, e.Location), new Rectangle(0, 0, shot.Width, shot.Height));
        if (r.Width < 5 || r.Height < 5) TooSmall = true; else Selection = r;
        Close();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Escape) Close();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        g.DrawImage(shot, 0, 0, shot.Width, shot.Height);
        var sel = Normalize(start, cur);
        bool hasSel = drag && sel.Width > 0 && sel.Height > 0;

        if (hasSel) g.ExcludeClip(sel);
        using (var dim = new SolidBrush(Color.FromArgb(120, 0, 0, 0))) g.FillRectangle(dim, 0, 0, Width, Height);
        g.ResetClip();
        if (!hasSel) return;

        using var pen = new Pen(Color.DeepSkyBlue, 2);
        g.DrawRectangle(pen, sel);

        var label = $"{sel.Width} × {sel.Height}";
        using var font = new Font("Segoe UI", 10f);
        var size = g.MeasureString(label, font);
        var pos = new PointF(sel.X, Math.Max(0, sel.Y - size.Height - 2));
        using var bg = new SolidBrush(Color.FromArgb(190, 0, 0, 0));
        g.FillRectangle(bg, pos.X, pos.Y, size.Width, size.Height);
        g.DrawString(label, font, Brushes.White, pos);
    }
}
