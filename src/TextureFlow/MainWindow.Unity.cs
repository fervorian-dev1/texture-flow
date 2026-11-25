using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace TextureFlow;

public sealed class AvatarTarget
{
 public string key {get;set;}="";public string label {get;set;}="";public string globalId {get;set;}="";public string materialGuid {get;set;}="";public string texturePath {get;set;}="";
 public int instanceId {get;set;} public int slot {get;set;} public int materialInstanceId {get;set;}
}
public sealed class UnityConnection
{
 public int protocolVersion {get;set;} public string sessionId {get;set;}="";public string generatedUtc {get;set;}="";public string message {get;set;}="";public bool busy {get;set;}public AvatarTarget[] targets {get;set;}=[];
}
public sealed class UnityReply
{
 public bool success {get;set;} public bool applied {get;set;}public string message {get;set;}="";public string receiptPath {get;set;}="";
}
public sealed class UnityCommand
{
 public int protocolVersion=1;public string expiresUtc=DateTime.UtcNow.AddMinutes(1).ToString("O");public string requestId=Guid.NewGuid().ToString("N"),sessionId="",action="apply",materialPath="",receiptPath="";public AvatarTarget? target;
}

public partial class MainWindow
{
 readonly DispatcherTimer connectionTimer=new(){Interval=TimeSpan.FromSeconds(1)};
 UnityConnection? connection;
 string? sourceTexturePath,lastReceipt,lastReceiptProject;
 string lastConnectionSignature="";
 bool viewReady;

 void ChangeStep(object sender,RoutedEventArgs e){Steps.SelectedIndex=int.Parse(((Button)sender).Tag.ToString()!);}
 void StartGuidedInterface()
 {
  viewReady=true;
  connectionTimer.Tick+=(_,_)=>{if(!busy)RefreshConnection();};connectionTimer.Start();
  Closed+=(_,_)=>connectionTimer.Stop();
  LoadRestoreState();RefreshConnection();
 }
 void ProjectChanged(object sender,SelectionChangedEventArgs e)
 {
  if(!viewReady)return;
  connection=null;Targets.ItemsSource=null;lastConnectionSignature="";LoadRestoreState();RefreshConnection();
 }
 void LoadRestoreState()
 {
  lastReceipt=null;lastReceiptProject=null;
  if(SelectedProject is not string project)return;
  try
  {
   string file=Path.Combine(project,"Library/TextureFlow/desktop-restore.json");
   if(!File.Exists(file))return;
   var reply=JsonSerializer.Deserialize<UnityReply>(File.ReadAllText(file),Json);
   if(reply==null||string.IsNullOrEmpty(reply.receiptPath))return;
   string path=Path.GetFullPath(Path.Combine(project,reply.receiptPath));
   string root=Path.GetFullPath(Path.Combine(project,"Assets/TextureFlow/Exports"))+Path.DirectorySeparatorChar;
   if(path.StartsWith(root,StringComparison.OrdinalIgnoreCase)&&File.Exists(path)){lastReceipt=reply.receiptPath;lastReceiptProject=project;}
  }
  catch(Exception ex)when(ex is IOException or JsonException or UnauthorizedAccessException){}
 }
 static UnityConnection? ReadConnection(string project)
 {
  try
  {
   string file=Path.Combine(project,"Library/TextureFlow/connection.json");
   if(!File.Exists(file)||DateTime.UtcNow-File.GetLastWriteTimeUtc(file)>TimeSpan.FromSeconds(10))return null;
   var state=JsonSerializer.Deserialize<UnityConnection>(File.ReadAllText(file),Json);
   return state?.protocolVersion==1?state:null;
  }
  catch(Exception ex)when(ex is IOException or JsonException or UnauthorizedAccessException){return null;}
 }
 void RefreshConnection()
 {
  if(!viewReady)return;
  connection=SelectedProject is string project?ReadConnection(project):null;
  RestoreButton.IsEnabled=!busy&&connection!=null&&lastReceipt!=null&&lastReceiptProject==SelectedProject;
  ConnectionLabel.Text=connection?.message??"Click Connect to Unity. Keep the project and avatar scene open in Unity.";
  if(connection==null){Targets.ItemsSource=null;lastConnectionSignature="";return;}
  string signature=connection.sessionId+"|"+string.Join("|",connection.targets.Select(t=>$"{t.key}:{t.materialInstanceId}:{t.label}:{t.texturePath}"));
  if(signature!=lastConnectionSignature)
  {
   string? selected=(Targets.SelectedItem as AvatarTarget)?.key;
   Targets.ItemsSource=connection.targets;
   Targets.SelectedItem=connection.targets.FirstOrDefault(t=>t.key==selected);
   if(Targets.SelectedItem==null&&!string.IsNullOrEmpty(sourceTexturePath)&&SelectedProject!=null)
   {
    string relative=Path.GetRelativePath(SelectedProject,sourceTexturePath).Replace('\\','/');
    var matches=connection.targets.Where(t=>string.Equals(t.texturePath,relative,StringComparison.OrdinalIgnoreCase)).ToArray();
    if(matches.Length==1)Targets.SelectedItem=matches[0];
   }
   lastConnectionSignature=signature;
  }
  RestoreButton.IsEnabled=!busy&&lastReceipt!=null&&lastReceiptProject==SelectedProject;
 }
 async Task<bool> InstallCompanionAsync(string project)
 {
  if(!ProjectTextures.IsProject(project))throw new IOException("Choose your Unity project first.");
  return await Task.Run(()=>
  {
   bool changed=false;string source=Path.Combine(AppContext.BaseDirectory,"UnityPackage");
   foreach(string file in Directory.GetFiles(source,"*",SearchOption.AllDirectories))
   {
    string destination=Path.Combine(project,"Assets/TextureFlow/Companion",Path.GetRelativePath(source,file));
    Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
    if(!File.Exists(destination)||!File.ReadAllBytes(destination).SequenceEqual(File.ReadAllBytes(file))){File.Copy(file,destination,true);changed=true;}
   }
   return changed;
  });
 }
 async Task<UnityConnection> EnsureConnectionAsync(string project)
 {
  bool changed=await InstallCompanionAsync(project);DateTime installed=DateTime.UtcNow;
  var deadline=DateTime.UtcNow.AddSeconds(35);
  while(DateTime.UtcNow<deadline)
  {
   var state=ReadConnection(project);
   if(state!=null&&!state.busy&&(!changed||DateTime.TryParse(state.generatedUtc,null,System.Globalization.DateTimeStyles.RoundtripKind,out var stamp)&&stamp.ToUniversalTime()>=installed.AddSeconds(-1)))return state;
   ConnectionLabel.Text=state?.message??"Waiting for Unity. Open this project in Unity and allow its import to finish.";
   await Task.Delay(400);
  }
  throw new IOException("The connector is installed. Open this project in Unity, click its window so it imports, leave Play mode, and click Connect again. No code editing is needed.");
 }
 async void ConnectUnity(object sender,RoutedEventArgs e)=>await Work(async()=>
 {
  string project=SelectedProject??throw new IOException("Choose a Unity project in step 1.");
  Status.Text="Connecting to Unity. The included companion is being set up automatically…";
  await EnsureConnectionAsync(project);RefreshConnection();Status.Text="Connected. Choose the avatar part in step 3, then click Create & Apply.";
 });
 public async Task<string> ExportAssetsAsync(string project,FlowSettings settings)
 {
  var workspace=CaptureWorkspace(true);
  string name="Flow-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N")[..6];
  string directory=Path.Combine(project,"Assets/TextureFlow/Exports",name);
  await InstallCompanionAsync(project);
  await Task.Run(()=>{var configuration=WorkspaceFiles.Write(directory,workspace,true);WriteJsonAtomic(Path.Combine(directory,"flow-request.json"),configuration);});
  return directory;
 }
 static void WriteJsonAtomic(string path,object value)
 {
  Directory.CreateDirectory(Path.GetDirectoryName(path)!);string temp=path+".tmp";
  File.WriteAllText(temp,JsonSerializer.Serialize(value,Json));File.Move(temp,path,true);
 }
 static async Task<UnityReply?> WaitForReplyAsync(string file,int seconds)
 {
  DateTime until=DateTime.UtcNow.AddSeconds(seconds);
  while(DateTime.UtcNow<until)
  {
   if(File.Exists(file))try{return JsonSerializer.Deserialize<UnityReply>(await File.ReadAllTextAsync(file),Json);}catch(Exception ex)when(ex is IOException or JsonException){}
   await Task.Delay(300);
  }
  return null;
 }
 public static async Task<UnityReply> SendUnityCommandAsync(string project,UnityCommand command)
 {
  string root=Path.Combine(project,"Library/TextureFlow");string result=Path.Combine(root,"results",command.requestId+".json");
  WriteJsonAtomic(Path.Combine(root,"requests",command.requestId+".json"),command);
  var reply=await WaitForReplyAsync(result,40);
  if(reply==null)
  {
   // Withdraw an unprocessed request so it cannot unexpectedly apply hours later.
   string request=Path.Combine(root,"requests",command.requestId+".json");
   try{File.Delete(request);}catch(IOException){}
   reply=await WaitForReplyAsync(result,2);
   if(reply==null)throw new IOException("Unity has not confirmed the change. The pending request was withdrawn; check Unity and reconnect before trying again.");
  }
  return reply;
 }
 async void CreateAndApply(object sender,RoutedEventArgs e)=>await Work(async()=>
 {
  _=CaptureWorkspace(true);
  string project=SelectedProject??throw new IOException("Choose your Unity project in step 1.");
  var requested=Targets.SelectedItem as AvatarTarget;
  Status.Text="Connecting and checking the chosen avatar part…";
  var state=await EnsureConnectionAsync(project);RefreshConnection();
  var target=requested==null?Targets.SelectedItem as AvatarTarget:state.targets.FirstOrDefault(t=>t.key==requested.key&&t.materialInstanceId==requested.materialInstanceId);
  if(target==null)throw new IOException("Choose an avatar part in step 3. If several parts use this texture, choose the intended one, such as your shoes.");
  var settings=Settings();_=(System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(settings.tint);
  Status.Text="Creating the textures and animated material. Waiting for Unity to finish importing…";
  string directory=await ExportAssetsAsync(project,settings);
  var imported=await WaitForReplyAsync(Path.Combine(directory,"flow-result.json"),45);
  if(imported==null)throw new IOException("Your files are saved, but Unity has not finished importing. Nothing has been assigned. Click Unity's window, wait for import to finish, then try again.");
  if(!imported.success)throw new IOException("Unity could not create the material: "+imported.message);
  Status.Text="Applying to "+target.label+"…";
  var reply=await SendUnityCommandAsync(project,new UnityCommand{sessionId=state.sessionId,target=target,materialPath=Path.GetRelativePath(project,Path.Combine(directory,"Animated.mat")).Replace('\\','/')});
  if(!reply.success)throw new IOException(reply.message);
  lastReceipt=reply.receiptPath;lastReceiptProject=project;WriteJsonAtomic(Path.Combine(project,"Library/TextureFlow/desktop-restore.json"),reply);RestoreButton.IsEnabled=true;Status.Text=reply.message;RefreshConnection();
 });
 async void RestoreMaterial(object sender,RoutedEventArgs e)=>await Work(async()=>
 {
  string project=SelectedProject??throw new IOException("Choose your Unity project first.");
  if(lastReceipt==null||lastReceiptProject!=project)throw new IOException("There is no saved material change to restore in this project.");
  var state=await EnsureConnectionAsync(project);
  var reply=await SendUnityCommandAsync(project,new UnityCommand{sessionId=state.sessionId,action="restore",receiptPath=lastReceipt});
  if(!reply.success)throw new IOException(reply.message);
  lastReceipt=null;WriteJsonAtomic(Path.Combine(project,"Library/TextureFlow/desktop-restore.json"),new UnityReply());Status.Text=reply.message;RestoreButton.IsEnabled=false;RefreshConnection();
 });
}
