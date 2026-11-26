using System.IO;
using System.Text.Json;
namespace TextureFlow;

public sealed class VoidAreaExport
{
 public string name="",maskFile="",textureFile="";
 public VoidLookSettings look=new();
}
public sealed class VoidExport
{
 public int schemaVersion=1;
 public VoidAreaExport[] areas=[];
}
public sealed class VoidSavedWork
{
 public int schemaVersion=1;
 public string paintingFile="";
 public VoidLookSettings[] looks=[];
}
public static class VoidFiles
{
 public static readonly JsonSerializerOptions Json=new(){IncludeFields=true,WriteIndented=true};
 public static void Atomic(string file,object data){Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(file))!);string temp=file+".tmp";File.WriteAllText(temp,JsonSerializer.Serialize(data,Json));File.Move(temp,file,true);}
 public static string Inside(string folder,string name){if(string.IsNullOrEmpty(name)||Path.IsPathRooted(name))throw new IOException("The saved pictures must stay beside this work file.");string path=Path.GetFullPath(Path.Combine(folder,name));if(!path.StartsWith(Path.GetFullPath(folder)+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new IOException("The saved pictures must stay beside this work file.");return path;}
 public static string Write(string project,WorkspaceSnapshot workspace,VoidLookSettings[] looks)
 {
  if(!ProjectTextures.IsProject(project))throw new IOException("Choose a Unity project folder containing Assets and ProjectSettings.");
  if(looks.Length!=workspace.Layers.Length)throw new IOException("Every opening needs a look setting.");
  foreach(var look in looks)look.Validate();
  var ready=WorkspaceFiles.ReadyLayers(workspace);string name="Void-"+DateTime.Now.ToString("yyyyMMdd-HHmmss")+"-"+Guid.NewGuid().ToString("N")[..6];string dir=Path.Combine(project,"Assets/TextureFlow/Exports",name);
  var settings=WorkspaceFiles.Write(dir,workspace,true);var areas=new List<VoidAreaExport>();
  for(int i=0;i<ready.Length;i++){int originalIndex=Array.IndexOf(workspace.Layers,ready[i]);areas.Add(new VoidAreaExport{name=ready[i].Name,maskFile=settings.layers[i].maskFile,textureFile=settings.layers[i].flowFile,look=looks[originalIndex].Copy()});}
  Atomic(Path.Combine(dir,"void-request.json"),new VoidExport{areas=areas.ToArray()});return dir;
 }
 public static async Task<UnityReply?> Wait(string file,int seconds){var until=DateTime.UtcNow.AddSeconds(seconds);while(DateTime.UtcNow<until){if(File.Exists(file))try{return JsonSerializer.Deserialize<UnityReply>(await File.ReadAllTextAsync(file),Json);}catch(Exception ex)when(ex is IOException or JsonException){}await Task.Delay(300);}return null;}
 public static async Task<UnityReply> Command(string project,UnityCommand command)
 {
  string root=Path.Combine(project,"Library/TextureFlow/Void");string file=Path.Combine(root,"requests",command.requestId+".json"),result=Path.Combine(root,"results",command.requestId+".json");Atomic(file,command);var reply=await Wait(result,45);if(reply==null){try{File.Delete(file);}catch(IOException){}reply=await Wait(result,2);if(reply==null)throw new IOException("Unity has not answered. The pending request was withdrawn. Check the current material in Unity before trying again.");}return reply;
 }
}
