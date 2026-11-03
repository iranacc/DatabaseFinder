using System.Drawing.Text;
using System.Runtime.InteropServices;

namespace DatabaseFinder;

public static class UiFont
{
    private static readonly PrivateFontCollection _pfc = new();
    private static readonly List<IntPtr> _handles = new();
    private static FontFamily? _persianFamily;
    private static readonly Dictionary<(float size, FontStyle style, bool isFa), Font> _cache = new();
    private static bool _initialized;
    private static readonly object _lock = new();

    public static void EnsureInitialized()
    {
        if (_initialized) return;
        lock (_lock)
        {
            if (_initialized) return;
            try
            {
                var asm = typeof(UiFont).Assembly;
                var resNames = asm.GetManifestResourceNames()
                    .Where(r => r.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase))
                    .OrderBy(r => r)
                    .ToList();

                foreach (var name in resNames)
                {
                    using var stream = asm.GetManifestResourceStream(name);
                    if (stream == null) continue;
                    var data = new byte[stream.Length];
                    stream.ReadExactly(data, 0, data.Length);
                    var handle = Marshal.AllocCoTaskMem(data.Length);
                    Marshal.Copy(data, 0, handle, data.Length);
                    _handles.Add(handle);
                    _pfc.AddMemoryFont(handle, data.Length);
                }

                _persianFamily = _pfc.Families.FirstOrDefault(f => f.Name.Contains("Vazir", StringComparison.OrdinalIgnoreCase));
            }
            catch
            {
                _persianFamily = null;
            }
            finally
            {
                _initialized = true;
            }
        }
    }

    public static Font Make(float size, FontStyle style = FontStyle.Regular)
    {
        EnsureInitialized();
        var isFa = L.IsFa;
        var key = (size, style, isFa);
        lock (_cache)
        {
            if (_cache.TryGetValue(key, out var cached)) return cached;

            Font font;
            if (isFa && _persianFamily != null)
            {
                try
                {
                    font = new Font(_persianFamily, size, style, GraphicsUnit.Point);
                }
                catch
                {
                    font = FallbackFont(size, style);
                }
            }
            else
            {
                font = FallbackFont(size, style);
            }

            _cache[key] = font;
            return font;
        }
    }

    private static Font FallbackFont(float size, FontStyle style)
    {
        try
        {
            return new Font("Segoe UI Variable Text", size, style, GraphicsUnit.Point);
        }
        catch
        {
            return new Font("Segoe UI", size, style, GraphicsUnit.Point);
        }
    }
}
