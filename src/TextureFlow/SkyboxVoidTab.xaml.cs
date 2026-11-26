using System.IO;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using Microsoft.Win32;
namespace TextureFlow;

public partial class SkyboxVoidTab : UserControl,IDisposable
{
 readonly MainWindow owner,editor;
 readonly ConditionalWeakTable<EffectLayer,VoidLookSettings> looks=new();
 readonly DispatcherTimer animation=new(){Interval=TimeSpan.FromMilliseconds(40)};
 readonly System.Diagnostics.Stopwatch watch=System.Diagnostics.Stopwatch.StartNew();
 bool ready,active,rendering,disposed;
 double time,previous,lastConnection;
 string connectionSignature="";
 string? restoreReceipt,restoreProject;
 public bool IsWorking{get;private set;}
 public MainWindow Tools=>editor;
 public SkyboxVoidTab(MainWindow owner)
 {
  this.owner=owner;editor=MainWindow.NewVoidTools(owner);InitializeComponent();ProjectsChoice.ItemsSource=editor.VoidProjects();ProjectsChoice.SelectedItem=owner.VoidProject??editor.VoidProject;if(ProjectsChoice.SelectedItem is string project)editor.VoidProject=project;Areas.ItemsSource=editor.EffectLayers;Areas.SelectedItem=editor.VoidArea;ready=true;LoadLook();UpdateBasics();LoadRestore();animation.Tick+=Tick;
 }
 public void SetActive(bool value){active=value;previous=watch.Elapsed.TotalSeconds;if(value){animation.Start();UpdateConnection();}else animation.Stop();}
 public VoidLookSettings LookFor(EffectLayer layer)=>looks.GetValue(layer,_=>new VoidLookSettings());
 void UpdateBasics()
 {
  try{var workspace=editor.VoidSnapshot();OriginalState.Text=$"Picture ready: {workspace.Original.W} x {workspace.Original.H}. Your original file is untouched.";Areas.SelectedItem=editor.VoidArea;var layer=editor.VoidArea;if(layer!=null){OpeningState.Text=layer.Mask.Any(v=>v>0)?"Opening ready. You can paint it again whenever you like.":"No opening yet. Click Paint my opening or use the whole picture.";SkyState.Text=layer.Flow==null?"No inside picture yet. Ready-made stars are an easy first try.":layer.FlowName+". "+(LookFor(layer).style==0&&layer.Flow.W!=layer.Flow.H*2?"This is not a 2:1 panorama; it may stretch in Sky window. Void/tunnel can use ordinary patterns.":"Picture ready.");}EmptyPreview.Visibility=Visibility.Collapsed;}
  catch{OriginalState.Text="No avatar picture yet.";OpeningState.Text="First choose the avatar picture in step 1.";SkyState.Text="First choose the avatar picture in step 1.";EmptyPreview.Visibility=Visibility.Visible;}
 }
 void LoadLook()
 {
  var layer=editor.VoidArea;if(layer==null)return;var s=LookFor(layer);ready=false;StyleChoice.SelectedIndex=s.style;Turn.Value=s.yawSpeed;Depth.Value=s.depth;Brightness.Value=s.brightness;Layers.Value=s.steps;Repeat.Value=s.tiling;Drift.Value=s.drift;StartTurn.Value=s.angle;Tint.Text=s.tint;WorldStill.IsChecked=s.worldLocked;ready=true;ExplainLook(s.style);
 }
 void SaveLook(){if(!ready||editor.VoidArea==null)return;var s=LookFor(editor.VoidArea);s.style=Math.Clamp(StyleChoice.SelectedIndex,0,2);s.yawSpeed=(float)Turn.Value;s.depth=(float)Depth.Value;s.brightness=(float)Brightness.Value;s.steps=(int)Layers.Value;s.tiling=(float)Repeat.Value;s.drift=(float)Drift.Value;s.angle=(float)StartTurn.Value;s.tint=Tint.Text;s.worldLocked=WorldStill.IsChecked==true;}
 void ExplainLook(int style){StyleHelp.Text=style==0?"Sky window: the picture is sampled by where you look, like a sky very far away. Depth controls are not needed.":style==1?"Deep void: dark parts let you see more layers behind the stars. Depth controls how far apart they feel.":"Tunnel: repeated layers turn and grow toward the dark middle. Use a pattern with bright shapes on a dark background.";Depth.IsEnabled=Layers.IsEnabled=Repeat.IsEnabled=Drift.IsEnabled=style!=0;WorldStill.IsEnabled=style==0;}
 void PresetChanged(object sender,SelectionChangedEventArgs e){if(!ready||editor.VoidArea==null)return;var layer=editor.VoidArea;looks.Remove(layer);looks.Add(layer,VoidLookSettings.Preset(StyleChoice.SelectedIndex));LoadLook();UpdateBasics();Message.Text="Look chosen. Move the preview's left/right slider to see inside.";}
 void LookChanged(object sender,RoutedEventArgs e){if(ready){SaveLook();UpdateBasics();}}
 void TintChanged(object sender,TextChangedEventArgs e){if(ready)SaveLook();}
 void AreaChanged(object sender,SelectionChangedEventArgs e){if(!ready||Areas.SelectedItem is not EffectLayer layer)return;editor.VoidArea=layer;LoadLook();UpdateBasics();}
 void ProjectChanged(object sender,SelectionChangedEventArgs e){if(!ready||ProjectsChoice.SelectedItem is not string project)return;editor.VoidProject=project;connectionSignature="";Targets.ItemsSource=null;LoadRestore();UpdateConnection();}
 async void Tick(object? sender,EventArgs e)
 {
  double now=watch.Elapsed.TotalSeconds;if(Play.IsChecked==true)time+=now-previous;previous=now;if(!active||disposed||IsWorking)return;
  if(now-lastConnection>1){lastConnection=now;UpdateConnection();}
  if(rendering)return;try{SaveLook();var workspace=editor.VoidSnapshot();var frames=workspace.Layers.Select((layer,index)=>(layer,index)).Where(p=>p.layer.Enabled&&p.layer.Flow!=null).Select(p=>new VoidFrame(p.layer.Flow!,p.layer.Mask,LookFor(editor.EffectLayers[p.index]).Copy())).ToArray();float yaw=(float)LookYaw.Value,pitch=(float)LookPitch.Value;double stamp=time;rendering=true;var raster=await Task.Run(()=>VoidPreview.Render(workspace.Original,frames,stamp,yaw,pitch));if(!disposed&&active)Preview.Source=raster.Bitmap();}catch(Exception ex){if(editor.VoidArea?.Flow!=null)Message.Text=ex.Message;}finally{rendering=false;}
 }
 bool ConfirmReplace(){try{var snapshot=editor.VoidSnapshot();if(!snapshot.Layers.Any(l=>l.Flow!=null||l.Mask.Any(v=>v>0)))return true;}catch{return true;}return MessageBox.Show(Window.GetWindow(this),"Start new openings here? Save sky work first if you want to keep this version. Your Scrolling textures tab is separate and will stay as it is.","Start a new sky / void picture?",MessageBoxButton.YesNo,MessageBoxImage.Question)==MessageBoxResult.Yes;}
 async Task Run(Func<Task> action)
 {
  if(IsWorking||disposed)return;IsWorking=true;WorkPanel.IsEnabled=false;try{await action();}catch(Exception ex){if(!disposed)Message.Text=ex.Message;}finally{IsWorking=false;if(!disposed){WorkPanel.IsEnabled=true;UpdateBasics();UpdateConnection();}}
 }
 async void OriginalFile(object sender,RoutedEventArgs e){var d=new OpenFileDialog{Filter=ProjectTextures.FileFilter};if(d.ShowDialog()==true&&ConfirmReplace())await Run(async()=>{await editor.VoidLoadPictureAsync(d.FileName,false);LoadLook();Message.Text="Picture ready. Next, paint where the opening should be.";});}
 async void SkyFile(object sender,RoutedEventArgs e){var d=new OpenFileDialog{Filter=ProjectTextures.FileFilter};if(d.ShowDialog()==true)await Run(async()=>{await editor.VoidLoadPictureAsync(d.FileName,true);Message.Text="Inside picture ready. Choose Sky window, Deep void or Tunnel.";});}
 async Task Browse(bool sky){if(editor.VoidProject is not string project)throw new IOException("First choose your Unity project in step 1.");var browser=new TextureBrowser(project){Owner=Window.GetWindow(this)};browser.UseVoidRole(sky);if(browser.ShowDialog()==true&&browser.ChosenPath!=null){if(!sky&&!ConfirmReplace())return;await editor.VoidLoadPictureAsync(browser.ChosenPath,sky);Message.Text=sky?"Inside picture ready.":"Avatar picture ready. Next paint the opening.";}}
 async void BrowseOriginal(object sender,RoutedEventArgs e)=>await Run(()=>Browse(false));
 async void BrowseSky(object sender,RoutedEventArgs e)=>await Run(()=>Browse(true));
 void ProjectFolder(object sender,RoutedEventArgs e){var d=new OpenFolderDialog{Title="Choose the Unity project folder (contains Assets and ProjectSettings)"};if(d.ShowDialog()==true){if(!ProjectTextures.IsProject(d.FolderName)){Message.Text="Choose the project root: it needs Assets and ProjectSettings folders.";return;}editor.VoidProject=d.FolderName;ProjectsChoice.ItemsSource=editor.VoidProjects();ProjectsChoice.SelectedItem=d.FolderName;connectionSignature="";LoadRestore();UpdateConnection();}}
 async void CopyScrolling(object sender,RoutedEventArgs e){if(ConfirmReplace())await Run(()=>{editor.VoidCopyFrom(owner.VoidSnapshot());LoadLook();GuideSteps.SelectedIndex=1;Message.Text="Copied your areas here. Scrolling work is untouched. Choose a sky/void look for each opening.";return Task.CompletedTask;});}
 async void Example(object sender,RoutedEventArgs e){if(ConfirmReplace())await Run(()=>{LoadExample();return Task.CompletedTask;});}
 public void LoadExample(){editor.VoidExample();LoadLook();GuideSteps.SelectedIndex=2;UpdateBasics();Message.Text="Example ready. Move Left/right to look through the opening. Try Deep void or Tunnel, then adjust the sliders.";}
 async void PaintOpening(object sender,RoutedEventArgs e)=>await Run(()=>{editor.VoidPaintOpening();Areas.SelectedItem=editor.VoidArea;LoadLook();Message.Text="Opening kept. Now choose the inside picture and look in step 3.";return Task.CompletedTask;});
 async void WholeOpening(object sender,RoutedEventArgs e)=>await Run(()=>{editor.VoidWholeOpening();Message.Text="The whole chosen material will become the opening. Next choose what is inside.";return Task.CompletedTask;});
 async void Stars(object sender,RoutedEventArgs e)=>await Run(()=>{editor.VoidUseStars();Message.Text="Stars ready. Try moving the look-around sliders.";return Task.CompletedTask;});
 void Next(object sender,RoutedEventArgs e){try{int step=int.Parse(((Button)sender).Tag.ToString()!);var snapshot=editor.VoidSnapshot();if(step>=2&&!snapshot.Layers.Any(l=>l.Enabled&&l.Mask.Any(v=>v>0)))throw new IOException("First paint an opening in step 2, or use the whole picture.");if(step>=3){_=WorkspaceFiles.ReadyLayers(snapshot);SaveLook();foreach(var l in editor.EffectLayers)LookFor(l).Validate();}GuideSteps.SelectedIndex=step;}catch(Exception ex){Message.Text=ex.Message;}}
 void UpdateConnection()
 {
  if(disposed)return;var state=editor.VoidConnection();ConnectionState.Text=state?.message??"Click Connect. Keep this project and your avatar's scene open in Unity.";string signature=state==null?"":state.sessionId+string.Join("|",state.targets.Select(t=>t.key+":"+t.materialInstanceId+":"+t.label));
  if(signature!=connectionSignature){connectionSignature=signature;string? key=(Targets.SelectedItem as AvatarTarget)?.key;Targets.ItemsSource=state?.targets??[];Targets.SelectedItem=state?.targets.FirstOrDefault(t=>t.key==key)??editor.VoidTarget;connectionSignature=signature;}
  bool complete=false;try{_=WorkspaceFiles.ReadyLayers(editor.VoidSnapshot());SaveLook();foreach(var layer in editor.EffectLayers)LookFor(layer).Validate();complete=true;}catch{}
  ApplyButton.IsEnabled=!IsWorking&&state!=null&&!state.busy&&Targets.SelectedItem is AvatarTarget&&complete;RestoreButton.IsEnabled=!IsWorking&&state!=null&&restoreReceipt!=null&&restoreProject==editor.VoidProject;
 }
 void TargetChanged(object sender,SelectionChangedEventArgs e){if(ready){editor.VoidTarget=Targets.SelectedItem as AvatarTarget;UpdateConnection();}}
 async void Connect(object sender,RoutedEventArgs e)=>await Run(async()=>{Message.Text="Connecting. Allow Unity to import the included connector; click its window if needed.";await editor.VoidConnectAsync();connectionSignature="";UpdateConnection();Message.Text="Connected. Choose the exact avatar part, then Create and apply.";});
 public async Task<string> ExportVoidAsync()
 {
  SaveLook();var workspace=editor.VoidSnapshot(true);var settings=editor.EffectLayers.Select(l=>LookFor(l).Copy()).ToArray();string project=editor.VoidProject??throw new IOException("Choose the Unity project in step 1.");await editor.VoidInstallAsync();return await Task.Run(()=>VoidFiles.Write(project,workspace,settings));
 }
 async void ExportOnly(object sender,RoutedEventArgs e)=>await Run(async()=>{var directory=await ExportVoidAsync();Message.Text="Files created: "+directory+". Unity will make Void.mat when it imports. Nothing was assigned.";});
 public async Task ApplyVoidAsync()
 {
  string project=editor.VoidProject??throw new IOException("Choose the Unity project in step 1.");var chosen=Targets.SelectedItem as AvatarTarget??throw new IOException("Choose the exact avatar part in step 4.");var state=await editor.VoidConnectAsync();if(!state.targets.Any(t=>t.key==chosen.key&&t.materialInstanceId==chosen.materialInstanceId))throw new IOException("That part changed. Connect again and choose it once more.");Message.Text="Making your sky / void material. Waiting for Unity to finish importing…";string directory=await ExportVoidAsync();var imported=await VoidFiles.Wait(Path.Combine(directory,"void-result.json"),50);if(imported==null)throw new IOException("Your files are ready but Unity has not answered. Click Unity's window, let it import, then reconnect. Nothing has been requested for assignment yet.");if(!imported.success)throw new IOException(imported.message);
  var reply=await VoidFiles.Command(project,new UnityCommand{sessionId=state.sessionId,target=chosen,action="apply",materialPath=Path.GetRelativePath(project,Path.Combine(directory,"Void.mat")).Replace('\\','/')});if(!reply.success)throw new IOException(reply.message);restoreReceipt=reply.receiptPath;restoreProject=project;VoidFiles.Atomic(Path.Combine(project,"Library/TextureFlow/Void/desktop-restore.json"),reply);Message.Text=reply.message;connectionSignature="";
 }
 async void Apply(object sender,RoutedEventArgs e)=>await Run(ApplyVoidAsync);
 public async Task RestoreVoidAsync(){string project=editor.VoidProject??throw new IOException("Choose the project first.");if(restoreReceipt==null||restoreProject!=project)throw new IOException("There is no saved sky / void material change to undo in this project.");var state=await editor.VoidConnectAsync();var reply=await VoidFiles.Command(project,new UnityCommand{sessionId=state.sessionId,action="restore",receiptPath=restoreReceipt});if(!reply.success)throw new IOException(reply.message);restoreReceipt=null;VoidFiles.Atomic(Path.Combine(project,"Library/TextureFlow/Void/desktop-restore.json"),new UnityReply());Message.Text=reply.message;connectionSignature="";}
 async void Restore(object sender,RoutedEventArgs e)=>await Run(RestoreVoidAsync);
 void LoadRestore(){restoreReceipt=null;restoreProject=null;if(editor.VoidProject is not string project)return;try{string file=Path.Combine(project,"Library/TextureFlow/Void/desktop-restore.json");if(!File.Exists(file))return;var state=JsonSerializer.Deserialize<UnityReply>(File.ReadAllText(file),VoidFiles.Json);if(state==null||string.IsNullOrEmpty(state.receiptPath))return;string path=VoidFiles.Inside(project,state.receiptPath);string root=Path.GetFullPath(Path.Combine(project,"Assets/TextureFlow/Exports"))+Path.DirectorySeparatorChar;if(path.StartsWith(root,StringComparison.OrdinalIgnoreCase)&&File.Exists(path)){restoreReceipt=state.receiptPath;restoreProject=project;}}catch{}}
 public async Task SaveVoidAsync(string file){SaveLook();var settings=editor.EffectLayers.Select(l=>LookFor(l).Copy()).ToArray();foreach(var s in settings)s.Validate();string painting=file+".painting-"+Guid.NewGuid().ToString("N")[..8]+".tflow";await editor.SaveWorkspaceAsync(painting);VoidFiles.Atomic(file,new VoidSavedWork{paintingFile=Path.GetFileName(painting),looks=settings});Message.Text="Sky work saved. Keep this file and its painting file/assets folder together.";}
 public async Task LoadVoidAsync(string file){string root=Path.GetDirectoryName(Path.GetFullPath(file))!;var data=JsonSerializer.Deserialize<VoidSavedWork>(File.ReadAllText(file),VoidFiles.Json)??throw new IOException("That sky work file is empty.");if(data.schemaVersion!=1||data.looks.Length<1||data.looks.Length>8)throw new IOException("Unsupported sky work file.");foreach(var s in data.looks)s.Validate();var painting=VoidFiles.Inside(root,data.paintingFile);var parsed=JsonDocument.Parse(File.ReadAllText(painting));string assets=VoidFiles.Inside(root,parsed.RootElement.GetProperty("assets").GetString()!);var workspace=await Task.Run(()=>WorkspaceFiles.Read(assets));if(workspace.Layers.Length!=data.looks.Length)throw new IOException("The saved openings and looks do not match.");editor.VoidCopyFrom(workspace);for(int i=0;i<data.looks.Length;i++){looks.Remove(editor.EffectLayers[i]);looks.Add(editor.EffectLayers[i],data.looks[i]);}Areas.SelectedItem=editor.VoidArea;LoadLook();UpdateBasics();GuideSteps.SelectedIndex=2;Message.Text="Sky work opened. Reconnect and choose the avatar part before applying.";}
 async void Save(object sender,RoutedEventArgs e){var d=new SaveFileDialog{Filter="Skybox / Void work|*.voidflow"};if(d.ShowDialog()==true)await Run(()=>SaveVoidAsync(d.FileName));}
 async void Open(object sender,RoutedEventArgs e){var d=new OpenFileDialog{Filter="Skybox / Void work|*.voidflow"};if(d.ShowDialog()==true&&ConfirmReplace())await Run(()=>LoadVoidAsync(d.FileName));}
 void OpenGuide(object sender,RoutedEventArgs e){try{string file=Path.Combine(AppContext.BaseDirectory,"Skybox Void Guide.html");if(!File.Exists(file))throw new IOException("Open Skybox Void Guide.html beside the portable app, or read the guide in the source folder.");System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(file){UseShellExecute=true});}catch(Exception ex){Message.Text=ex.Message;}}
 public void Dispose(){if(disposed)return;disposed=true;active=false;animation.Stop();editor.VoidDisposeTools();}
}
