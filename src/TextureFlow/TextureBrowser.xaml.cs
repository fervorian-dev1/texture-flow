using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace TextureFlow;

public sealed class TextureEntry : INotifyPropertyChanged
{
    public string Path { get; }
    public string RelativePath { get; }
    public string Name => System.IO.Path.GetFileName(Path);
    string detail;
    BitmapSource? thumbnail;
    public bool Visible { get; set; }
    public BitmapSource? Thumbnail { get => thumbnail; set { thumbnail = value; Changed(); } }
    public string Detail { get => detail; set { detail = value; Changed(); } }
    public TextureEntry(string path, string relativePath) { Path = path; RelativePath = relativePath; detail = System.IO.Path.GetExtension(path).ToUpperInvariant(); }
    public event PropertyChangedEventHandler? PropertyChanged;
    void Changed([CallerMemberName] string? property = null) => PropertyChanged?.Invoke(this, new(property));
}

public partial class TextureBrowser : Window
{
    readonly string project;
    CancellationTokenSource scan = new();
    readonly CancellationTokenSource lifetime = new();
    readonly SemaphoreSlim decoders = new(2);
    ListCollectionView? view;
    bool closed;
    public string? ChosenPath { get; private set; }
    public bool ChosenAsFlow { get; private set; }

    public TextureBrowser(string project)
    {
        this.project = project;
        InitializeComponent();
        ProjectLabel.Text = project;
        Loaded += async (_, _) => await ReloadAsync();
        Closed += (_, _) => { closed = true; scan.Cancel(); lifetime.Cancel(); };
    }

    public async Task ReloadAsync()
    {
        scan.Cancel();
        scan = new();
        var current = scan;
        Results.ItemsSource = null;
        view = null;
        Status.Text = "Searching project textures…";
        bool includePackages = Packages.IsChecked == true;
        var progress = new Progress<string>(message => { if (scan == current && !closed) Status.Text = message; });
        try
        {
            var items = await Task.Run(() => ProjectTextures.Scan(project, includePackages, current.Token, progress));
            if (current.IsCancellationRequested || closed) return;
            view = new ListCollectionView(items);
            view.Filter = Matches;
            Results.ItemsSource = view;
            UpdateCount();
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { if (scan == current && !closed) Status.Text = "Could not search: " + ex.Message; }
    }

    bool Matches(object item) => item is TextureEntry entry && Search.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries).All(term => entry.RelativePath.Contains(term, StringComparison.OrdinalIgnoreCase));
    void UpdateCount() { if (view != null) Status.Text = $"{view.Count:N0} matching textures • {view.SourceCollection.Cast<object>().Count():N0} total"; }
    void SearchChanged(object sender, TextChangedEventArgs e) { view?.Refresh(); UpdateCount(); }
    async void Refresh(object sender, RoutedEventArgs e) => await ReloadAsync();
    void Selected(object sender, SelectionChangedEventArgs e) { BaseButton.IsEnabled = FlowButton.IsEnabled = Results.SelectedItem is TextureEntry; }
    void Choose(bool flow) { if (Results.SelectedItem is not TextureEntry entry) return; ChosenPath = entry.Path; ChosenAsFlow = flow; DialogResult = true; }
    void UseBase(object sender, RoutedEventArgs e) => Choose(false);
    void UseFlow(object sender, RoutedEventArgs e) => Choose(true);
    void DoubleClick(object sender, MouseButtonEventArgs e) { if (e.OriginalSource is DependencyObject origin && ItemsControl.ContainerFromElement(Results, origin) is ListBoxItem) Choose(false); }
    async void RowLoaded(object sender, RoutedEventArgs e)
    {
        if (((FrameworkElement)sender).DataContext is not TextureEntry entry) return;
        entry.Visible = true;
        try
        {
            await decoders.WaitAsync(lifetime.Token);
            try
            {
                if (!entry.Visible || closed) return;
                var thumbnail = await Task.Run(() => ProjectTextures.Thumbnail(entry.Path), lifetime.Token);
                if (entry.Visible && !closed) { entry.Thumbnail = thumbnail; entry.Detail = System.IO.Path.GetExtension(entry.Path).ToUpperInvariant() + " • " + new FileInfo(entry.Path).Length.ToString("N0") + " bytes"; }
            }
            finally { decoders.Release(); }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { if (!closed) entry.Detail = "Preview unavailable: " + ex.Message; }
    }
    void RowUnloaded(object sender, RoutedEventArgs e) { if (((FrameworkElement)sender).DataContext is TextureEntry entry) { entry.Visible = false; entry.Thumbnail = null; } }
}
