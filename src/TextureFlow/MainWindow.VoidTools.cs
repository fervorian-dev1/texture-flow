using System.IO;
using System.Windows;
using System.Windows.Controls;
namespace TextureFlow;

public partial class MainWindow
{
 bool voidToolConfigured,voidToolMayClose;
 public static MainWindow NewVoidTools(MainWindow owner)
 {
  var editor=new MainWindow{Owner=owner,ShowInTaskbar=false,Title="Paint your opening - Texture Flow"};editor.SetValue(SkyboxVoidBootstrap.SuppressProperty,true);editor.ConfigureVoidTools();return editor;
 }
 void ConfigureVoidTools()
 {
  if(voidToolConfigured)return;voidToolConfigured=true;timer.Stop();Playing.IsChecked=false;ShowFlow.IsChecked=false;ShowGlow.IsChecked=false;ShowMask.IsChecked=true;
  ((TabItem)Steps.Items[0]).Visibility=Visibility.Collapsed;((TabItem)Steps.Items[2]).Visibility=Visibility.Collapsed;((TabItem)Steps.Items[1]).Header="Paint your opening";Steps.SelectedIndex=1;
  if(Content is DockPanel root){foreach(var child in root.Children.OfType<StackPanel>().Where(p=>DockPanel.GetDock(p)==Dock.Top))child.Visibility=Visibility.Collapsed;
   var footer=new StackPanel{Margin=new Thickness(4)};footer.Children.Add(new TextBlock{Text="1. Click a color to fill it.  2. Use Paint add / Paint erase to tidy it.  3. Click Done to see inside.",Foreground=System.Windows.Media.Brushes.White,TextWrapping=TextWrapping.Wrap});var done=new Button{Content="Done - see my sky / void",Padding=new Thickness(18,10,18,10),HorizontalAlignment=HorizontalAlignment.Right};done.Click+=(_,_)=>Hide();footer.Children.Add(done);DockPanel.SetDock(footer,Dock.Bottom);root.Children.Insert(0,footer);
  }
  bool motion=false;foreach(UIElement child in EffectsPanel.Children){if(child is TextBlock t&&t.Text.StartsWith("Movement direction",StringComparison.Ordinal))motion=true;if(motion&&!(child is Expander ex&&ex.Header?.ToString()?.StartsWith("Advanced selection",StringComparison.Ordinal)==true))child.Visibility=Visibility.Collapsed;}
  foreach(var button in Descendants<Button>(Content as DependencyObject).Where(b=>b.Content is string text&&(text=="Back to textures"||text=="Next: apply")))button.Visibility=Visibility.Collapsed;
  Closing+=(_,e)=>{if(!voidToolMayClose){e.Cancel=true;Hide();}};
 }
 static IEnumerable<T> Descendants<T>(DependencyObject? root)where T:DependencyObject{if(root==null)yield break;foreach(var child in LogicalTreeHelper.GetChildren(root)){if(child is T value)yield return value;if(child is DependencyObject d)foreach(var nested in Descendants<T>(d))yield return nested;}}
 public void VoidPaintOpening(){Check();ConfigureVoidTools();ShowMask.IsChecked=true;ShowFlow.IsChecked=false;Before.IsChecked=false;Steps.SelectedIndex=1;timer.Start();try{ShowDialog();SaveActiveEffect();}finally{timer.Stop();}}
 public void VoidDisposeTools(){voidToolMayClose=true;Close();}
 public string? VoidProject{get=>SelectedProject;set{if(value!=null)SelectProject(value);}}
 public string[] VoidProjects()=>Projects.Items.Cast<string>().ToArray();
 public AvatarTarget[] VoidTargets()=>connection?.targets??[];
 public AvatarTarget? VoidTarget{get=>Targets.SelectedItem as AvatarTarget;set=>Targets.SelectedItem=value;}
 public EffectLayer? VoidArea{get=>activeEffect;set{if(value!=null)Effects.SelectedItem=value;}}
 public UnityConnection? VoidConnection(){RefreshConnection();return connection;}
 public Task<bool> VoidInstallAsync()=>InstallCompanionAsync(SelectedProject??throw new IOException("Choose your Unity project first."));
 public async Task<UnityConnection> VoidConnectAsync(){string project=SelectedProject??throw new IOException("First choose the Unity project that contains your avatar.");return await EnsureConnectionAsync(project);}
 public WorkspaceSnapshot VoidSnapshot(bool ready=false)=>CaptureWorkspace(ready);
 public async Task VoidLoadPictureAsync(string file,bool sky){await LoadTextureAsync(file,sky);ShowFlow.IsChecked=false;ShowMask.IsChecked=true;timer.Stop();}
 public void VoidUseStars(){Check();SetMovingTexture(VoidPreview.Stars(),"Ready-made stars");SaveActiveEffect();}
 public void VoidWholeOpening(){Check();Remember();mask=Enumerable.Repeat((byte)255,image!.W*image.H).ToArray();SaveActiveEffect();ShowMask.IsChecked=true;}
 public void VoidCopyFrom(WorkspaceSnapshot workspace)
 {
  SetImage(new Raster(workspace.Original.W,workspace.Original.H,(byte[])workspace.Original.Pixels.Clone()));changingEffects=true;EffectLayers.Clear();foreach(var l in workspace.Layers)EffectLayers.Add(new EffectLayer{Name=l.Name,Enabled=l.Enabled,Mask=(byte[])l.Mask.Clone(),Flow=l.Flow,Settings=CopySettings(l.Settings),FlowName=l.FlowName});var selected=EffectLayers[Math.Clamp(workspace.ActiveLayer,0,EffectLayers.Count-1)];Effects.SelectedItem=selected;nextArea=EffectLayers.Count+1;changingEffects=false;ActivateEffect(selected);ShowFlow.IsChecked=false;ShowMask.IsChecked=true;timer.Stop();
 }
 public void VoidExample()
 {
  var pixels=new byte[512*512*4];for(int y=0;y<512;y++)for(int x=0;x<512;x++){int i=(y*512+x)*4;bool opening=x>110&&x<402&&y>86&&y<426;byte v=opening?(byte)8:(byte)(145+((x/24+y/24)%2)*18);pixels[i]=v;pixels[i+1]=v;pixels[i+2]=v;pixels[i+3]=255;}
  SetImage(new Raster(512,512,pixels));activeEffect!.Name="My space window";mask=Selection.Select(image!,256,256,0,true,false,0,0,0);VoidUseStars();SaveActiveEffect();timer.Stop();
 }
}

public partial class TextureBrowser
{
 public void UseVoidRole(bool sky)
 {
  Title=sky?"Pick the picture inside your opening":"Pick your original avatar picture";
  BaseButton.Visibility=sky?Visibility.Collapsed:Visibility.Visible;FlowButton.Visibility=sky?Visibility.Visible:Visibility.Collapsed;
  if(sky)FlowButton.Content="Use this sky / inside picture";else BaseButton.Content="Use this avatar picture";
  Results.PreviewMouseDoubleClick+=(_,e)=>{e.Handled=true;Choose(sky);};
 }
}
