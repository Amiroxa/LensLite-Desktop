using System.Drawing;
using System.Drawing.Imaging;
using Tesseract;

namespace LensLite;

sealed class Ocr : IDisposable
{
    readonly string dataPath = Path.Combine(AppContext.BaseDirectory, "tessdata");
    Engine? engine = null;
    string? lang = null;

    public string Recognize(Bitmap crop, string language)
    {
        if (lang != language)
        {
            this.lang = language;
            Dispose();
            var dataPath = Path.Combine(AppContext.BaseDirectory, "tessdata");
            engine = new Engine(dataPath, language);
        }

        using var ms = new MemoryStream();
        crop.Save(ms, ImageFormat.Png);
        using var pix = Pix.Image.LoadFromMemory(ms.ToArray());
        using var page = engine.Process(pix);
        return Clean(page.Text);
    }

    static string Clean(string raw)
    {
        var lines = raw.Replace("\r", "").Split('\n')
            .Select(l => l.TrimEnd().Replace('ي', 'ی').Replace('ك', 'ک'));
        return string.Join(Environment.NewLine, lines).Trim();
    }

    public void Dispose() => engine?.Dispose();
}
