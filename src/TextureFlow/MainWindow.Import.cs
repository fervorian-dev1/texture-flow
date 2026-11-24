using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TextureFlow;

public partial class MainWindow
{
    string? SelectedProject => Projects.SelectedItem as string;

    void SelectProject(string project)
    {
        var existing = Projects.Items.Cast<string>().FirstOrDefault(p => string.Equals(p, project, StringComparison.OrdinalIgnoreCase));
        if (existing == null) Projects.Items.Add(project);
        Projects.SelectedItem = existing ?? project;
    }

    async void BrowseTextures(object sender, RoutedEventArgs e)
    {
        if (busy) return;
        if (SelectedProject is not string project || !ProjectTextures.IsProject(project))
        {
            Status.Text = "Choose a Unity project first, or drop its folder onto Texture Flow.";
            return;
        }
        var browser = new TextureBrowser(project) { Owner = this };
        bool wasPlaying = Playing.IsChecked == true;
        Playing.IsChecked = false;
        timer.Stop();
        try
        {
            if (browser.ShowDialog() == true && browser.ChosenPath != null)
                await Work(() => LoadTextureAsync(browser.ChosenPath, browser.ChosenAsFlow));
        }
        finally { Playing.IsChecked = wasPlaying; previous = watch.Elapsed.TotalSeconds; timer.Start(); }
    }

    async Task LoadTextureAsync(string path, bool asFlow)
    {
        var raster = await Task.Run(() => Raster.Load(path));
        string? project = ProjectTextures.FindProject(path);
        if (project != null) SelectProject(project);
        if (asFlow) flow = raster;
        else { SetImage(raster);sourceTexturePath=Path.GetFullPath(path);lastConnectionSignature=""; }
        if(asFlow)FlowLabel.Text=Path.GetFileName(path);else BaseLabel.Text=Path.GetFileName(path);
        RefreshConnection();
        Status.Text = $"{(asFlow ? "Animated" : "Base")} texture: {Path.GetFileName(path)} • {raster.W} × {raster.H}";
    }

    // All drop types share the same import path; VCC settings and source textures are read-only.
    public async Task ImportDropAsync(IDataObject data, bool asFlow)
    {
        var paths = ProjectTextures.DropPaths(data, SelectedProject);
        var textures = paths.Where(p => File.Exists(p) && ProjectTextures.IsTexture(p)).ToArray();
        if (textures.Length > 2) throw new IOException("Drop one texture, or two textures for base + animation. Use the project browser for larger collections.");
        if (textures.Length == 2)
        {
            // Decode both before replacing either layer, so a failed second image leaves the session intact.
            var first = await Task.Run(() => Raster.Load(textures[0]));
            var second = await Task.Run(() => Raster.Load(textures[1]));
            SetImage(first); flow = second;sourceTexturePath=Path.GetFullPath(textures[0]);BaseLabel.Text=Path.GetFileName(textures[0]);FlowLabel.Text=Path.GetFileName(textures[1]);lastConnectionSignature="";
            var root = ProjectTextures.FindProject(textures[0]);
            if (root != null) SelectProject(root);
            Status.Text = $"Loaded base: {Path.GetFileName(textures[0])} • animation: {Path.GetFileName(textures[1])}";
            return;
        }
        if (textures.Length == 1) { await LoadTextureAsync(textures[0], asFlow); return; }
        foreach (string path in paths)
        {
            var project = ProjectTextures.FindProject(path);
            if (project == null) continue;
            SelectProject(project);
            Status.Text = "Selected project: " + project + " • Click Browse project textures to see its images.";
            return;
        }
        throw new IOException("Drop a local texture, Unity project folder, or project/texture path. The source must provide a file or path drag payload.");
    }

    bool AcceptsDrop(IDataObject data) => ProjectTextures.DropPaths(data, SelectedProject).Any(p => ProjectTextures.IsTexture(p) || ProjectTextures.FindProject(p) != null);

    void IncomingDrag(object sender, DragEventArgs e)
    {
        e.Handled = true;
        e.Effects = !busy && (e.AllowedEffects & DragDropEffects.Copy) != 0 && AcceptsDrop(e.Data) ? DragDropEffects.Copy : DragDropEffects.None;
        DropBanner.BorderBrush = new SolidColorBrush(e.Effects == DragDropEffects.Copy ? Color.FromRgb(64, 223, 255) : Color.FromRgb(82, 100, 127));
    }

    void LeavingDrag(object sender, DragEventArgs e) => DropBanner.BorderBrush = new SolidColorBrush(Color.FromRgb(82, 100, 127));

    async void IncomingDrop(object sender, DragEventArgs e)
    {
        e.Handled = true;
        LeavingDrag(sender, e);
        bool asFlow = (e.KeyStates & DragDropKeyStates.ShiftKey) != 0;
        for (var current = e.OriginalSource as DependencyObject; current != null; current = current is Visual ? VisualTreeHelper.GetParent(current) : LogicalTreeHelper.GetParent(current))
            if (current is FrameworkElement element && element.Name == "FlowDrop") { asFlow = true; break; }
        await Work(() => ImportDropAsync(e.Data, asFlow));
    }
}
