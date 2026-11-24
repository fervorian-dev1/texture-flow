using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ImageMagick;

namespace TextureFlow;

public static class ProjectTextures
{
    public static readonly HashSet<string> Extensions = new(StringComparer.OrdinalIgnoreCase)
    { ".png", ".jpg", ".jpeg", ".bmp", ".tif", ".tiff", ".tga", ".psd", ".psb", ".exr", ".hdr", ".dds", ".gif", ".webp", ".iff", ".pict" };
    public const string FileFilter = "Textures|*.png;*.jpg;*.jpeg;*.bmp;*.tif;*.tiff;*.tga;*.psd;*.psb;*.exr;*.hdr;*.dds;*.gif;*.webp;*.iff;*.pict";

    public static bool IsTexture(string path) => Extensions.Contains(Path.GetExtension(path));
    public static bool IsProject(string path) => Directory.Exists(Path.Combine(path, "Assets")) && Directory.Exists(Path.Combine(path, "ProjectSettings"));

    public static string? FindProject(string path)
    {
        try
        {
            var dir = new DirectoryInfo(Directory.Exists(path) ? path : Path.GetDirectoryName(path)!);
            while (dir != null) { if (IsProject(dir.FullName)) return dir.FullName; dir = dir.Parent; }
        }
        catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException) { }
        return null;
    }

    public static IReadOnlyList<string> DropPaths(IDataObject data, string? project)
    {
        var result = new List<string>();
        void Add(string value)
        {
            value = value.Trim().Trim('"');
            if (Uri.TryCreate(value, UriKind.Absolute, out var uri) && uri.IsFile) value = uri.LocalPath;
            try
            {
                if (!Path.IsPathRooted(value))
                {
                    if (project == null || !(value.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase) || value.StartsWith("Assets\\", StringComparison.OrdinalIgnoreCase) || value.StartsWith("Packages/", StringComparison.OrdinalIgnoreCase))) return;
                    value = Path.Combine(project, value);
                }
                value = Path.GetFullPath(value);
                if ((File.Exists(value) || Directory.Exists(value)) && !result.Contains(value, StringComparer.OrdinalIgnoreCase)) result.Add(value);
            }
            catch (Exception ex) when (ex is ArgumentException or IOException or UnauthorizedAccessException) { }
        }
        if (data.GetDataPresent(DataFormats.FileDrop) && data.GetData(DataFormats.FileDrop) is string[] paths)
            foreach (var path in paths) Add(path);
        foreach (var format in new[] { DataFormats.UnicodeText, DataFormats.Text, "text/uri-list" })
        {
            if (!data.GetDataPresent(format) || data.GetData(format) is not string text) continue;
            foreach (var line in text.Split(new[] { '\r', '\n', '\0' }, StringSplitOptions.RemoveEmptyEntries)) Add(line);
            try
            {
                using var json = JsonDocument.Parse(text);
                if (json.RootElement.ValueKind == JsonValueKind.Object)
                    foreach (var key in new[] { "path", "projectPath", "projectDirectory", "filePath" })
                        if (json.RootElement.TryGetProperty(key, out var item) && item.ValueKind == JsonValueKind.String) Add(item.GetString()!);
            }
            catch (JsonException) { }
        }
        return result;
    }

    public static List<TextureEntry> Scan(string project, bool packages, CancellationToken cancellation, IProgress<string>? progress = null)
    {
        if (!IsProject(project)) throw new IOException("Select a Unity project root containing Assets and ProjectSettings.");
        var roots = new List<string> { Path.Combine(project, "Assets") };
        if (packages)
        {
            roots.Add(Path.Combine(project, "Packages"));
            // Only resolved cached packages belong to this project; stale cache versions are excluded.
            string cache = Path.Combine(project, "Library", "PackageCache");
            string lockFile = Path.Combine(project, "Packages", "packages-lock.json");
            if (File.Exists(lockFile))
            {
                using var json = JsonDocument.Parse(File.ReadAllText(lockFile));
                if (json.RootElement.TryGetProperty("dependencies", out var deps))
                    foreach (var dep in deps.EnumerateObject())
                    {
                        cancellation.ThrowIfCancellationRequested();
                        var item = dep.Value;
                        if (item.TryGetProperty("source", out var source) && source.GetString() == "local" && item.TryGetProperty("version", out var local) && local.GetString() is string version && version.StartsWith("file:"))
                            roots.Add(Path.GetFullPath(Path.Combine(project, "Packages", version[5..])));
                        foreach (var dir in Directory.Exists(cache) ? Directory.EnumerateDirectories(cache, dep.Name + "@*") : Array.Empty<string>())
                        {
                            string suffix = Path.GetFileName(dir)[(dep.Name.Length + 1)..];
                            if (item.TryGetProperty("version", out var v) && (suffix == v.GetString() || (item.TryGetProperty("hash", out var hash) && hash.GetString() is string h && h.StartsWith(suffix, StringComparison.OrdinalIgnoreCase)))) roots.Add(dir);
                        }
                    }
            }
        }
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var found = new List<TextureEntry>();
        var stack = new Stack<string>(roots.Where(Directory.Exists));
        int skipped = 0;
        while (stack.TryPop(out var dir))
        {
            cancellation.ThrowIfCancellationRequested();
            if (!seen.Add(Path.GetFullPath(dir))) continue;
            try
            {
                foreach (var file in Directory.EnumerateFiles(dir))
                {
                    cancellation.ThrowIfCancellationRequested();
                    if (IsTexture(file)) found.Add(new TextureEntry(file, Path.GetRelativePath(project, file)));
                }
                foreach (var child in Directory.EnumerateDirectories(dir))
                    if ((File.GetAttributes(child) & FileAttributes.ReparsePoint) == 0) stack.Push(child);
                if (seen.Count % 50 == 0) progress?.Report($"Searching… {found.Count:N0} textures found");
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { skipped++; }
        }
        progress?.Report($"{found.Count:N0} textures found" + (skipped > 0 ? $" • {skipped} inaccessible folders skipped" : ""));
        return found.OrderBy(e => e.RelativePath, StringComparer.OrdinalIgnoreCase).ToList();
    }

    public static BitmapSource Thumbnail(string path)
    {
        using var image = new MagickImage();
        image.Read(path, new MagickReadSettings { FrameIndex = 0, FrameCount = 1 });
        image.AutoOrient();
        image.Thumbnail(128, 128);
        using var stream = new MemoryStream(image.ToByteArray(MagickFormat.Png));
        var bitmap = BitmapFrame.Create(stream, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
        bitmap.Freeze();
        return bitmap;
    }
}
