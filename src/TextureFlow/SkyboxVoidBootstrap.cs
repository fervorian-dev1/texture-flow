using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Controls;
namespace TextureFlow;

public static class SkyboxVoidBootstrap
{
 static readonly ConditionalWeakTable<MainWindow,object> attached=new();
 public static readonly DependencyProperty SuppressProperty=DependencyProperty.RegisterAttached("Suppress",typeof(bool),typeof(SkyboxVoidBootstrap),new PropertyMetadata(false));
 [ModuleInitializer] public static void Register()=>EventManager.RegisterClassHandler(typeof(MainWindow),FrameworkElement.LoadedEvent,new RoutedEventHandler(Loaded));
 static void Loaded(object sender,RoutedEventArgs e)
 {
  if(sender is not MainWindow main||!ReferenceEquals(e.OriginalSource,main)||(bool)main.GetValue(SuppressProperty)||attached.TryGetValue(main,out _))return;
  attached.Add(main,new object());main.AttachSkyboxVoidTab();
 }
}
public partial class MainWindow
{
 public void AttachSkyboxVoidTab()
 {
  if(Content is not UIElement legacy)return;
  Content=null;var tabs=new TabControl{Background=System.Windows.Media.Brushes.Transparent,BorderThickness=new Thickness(0)};
  var original=new TabItem{Header="Scrolling textures",Foreground=System.Windows.Media.Brushes.Black,Content=new ContentControl{Content=legacy,Foreground=this.Foreground,HorizontalContentAlignment=HorizontalAlignment.Stretch,VerticalContentAlignment=VerticalAlignment.Stretch}};var feature=new TabItem{Header="Skybox / Void",Foreground=System.Windows.Media.Brushes.Black};tabs.Items.Add(original);tabs.Items.Add(feature);Content=tabs;
  SkyboxVoidTab? control=null;bool reverting=false;
  tabs.SelectionChanged+=(_,e)=>{if(reverting||!ReferenceEquals(e.Source,tabs))return;if(busy||control?.IsWorking==true){reverting=true;tabs.SelectedItem=e.RemovedItems.Count>0?e.RemovedItems[0]:original;reverting=false;return;}if(tabs.SelectedItem==feature){timer.Stop();control??=new SkyboxVoidTab(this);feature.Content=control;control.SetActive(true);}else{control?.SetActive(false);previous=watch.Elapsed.TotalSeconds;timer.Start();}};
  Closing+=(_,e)=>{if(control?.IsWorking==true){e.Cancel=true;MessageBox.Show(this,"Please wait for the current sky / void operation to finish before closing.","Texture Flow");return;}control?.Dispose();};
 }
}
