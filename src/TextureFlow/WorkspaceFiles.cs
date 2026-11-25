using System.IO;
using System.Text.Json;
using System.Windows;
namespace TextureFlow;

public sealed record LayerSnapshot(string Name,bool Enabled,byte[] Mask,Raster? Flow,MotionSettings Settings,string FlowName);
public sealed record WorkspaceSnapshot(Raster Original,LayerSnapshot[] Layers,int ActiveLayer,string Shader);

public static class WorkspaceFiles
{
 public static void ValidateMotion(MotionSettings s)
 {
  if(s==null)throw new IOException("Area settings are missing.");
  bool Range(float v,float min,float max)=>float.IsFinite(v)&&v>=min&&v<=max;
  if(!Range(s.speed,.01f,100)||!Range(s.angle,0,360)||!Range(s.tilingX,.01f,1000)||!Range(s.tilingY,.01f,1000)||!Range(s.rotation,0,360)||!Range(s.emission,0,100)||!Range(s.hdr,-10,16))throw new IOException("An area contains invalid motion or glow settings.");
  try{_=(System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString(s.tint);}catch{throw new IOException("An area contains an invalid color. Use #RRGGBB.");}
 }
 public static LayerSnapshot[] ReadyLayers(WorkspaceSnapshot workspace)
 {
  var ready=new List<LayerSnapshot>();
  foreach(var layer in workspace.Layers.Where(l=>l.Enabled)){
   bool selected=layer.Mask.Any(v=>v>0);
   if(!selected&&layer.Flow==null)continue;
   if(!selected)throw new IOException($"{layer.Name}: select or paint the area that should animate.");
   if(layer.Flow==null)throw new IOException($"{layer.Name}: choose a moving texture. Other areas are kept.");
   ValidateMotion(layer.Settings);ready.Add(layer);
  }
  if(ready.Count==0)throw new IOException("Create at least one enabled area with a selection and moving texture.");
  return ready.ToArray();
 }
 static void SaveMask(Raster original,byte[] mask,string file){var pixels=new byte[mask.Length*4];for(int i=0;i<mask.Length;i++){pixels[i*4]=pixels[i*4+1]=pixels[i*4+2]=mask[i];pixels[i*4+3]=255;}new Raster(original.W,original.H,pixels).Save(file);}
 public static FlowSettings Write(string directory,WorkspaceSnapshot workspace,bool export)
 {
  var layers=export?ReadyLayers(workspace):workspace.Layers;
  if(layers.Length<1||layers.Length>MainWindow.MaxEffects)throw new IOException("Unsupported number of areas.");
  Directory.CreateDirectory(directory);workspace.Original.Save(Path.Combine(directory,"base.png"));var configs=new List<LayerConfig>();var combined=new byte[workspace.Original.W*workspace.Original.H];
  for(int n=0;n<layers.Length;n++){
   var layer=layers[n];if(layer.Mask.Length!=combined.Length)throw new IOException("Area mask dimensions do not match.");ValidateMotion(layer.Settings);
   string child=$"layer-{n+1:00}";Directory.CreateDirectory(Path.Combine(directory,child));string maskFile=child+"/mask.png",flowFile=layer.Flow==null?"":child+"/flow.png";
   SaveMask(workspace.Original,layer.Mask,Path.Combine(directory,maskFile));if(layer.Flow!=null)layer.Flow.Save(Path.Combine(directory,flowFile));
   if(layer.Enabled&&layer.Flow!=null)for(int i=0;i<combined.Length;i++)combined[i]=(byte)(255-(255-combined[i])*(255-layer.Mask[i])/255);
   configs.Add(new LayerConfig{name=layer.Name,enabled=layer.Enabled,settings=layer.Settings,maskFile=maskFile,flowFile=flowFile,flowName=layer.FlowName});
  }
  SaveMask(workspace.Original,combined,Path.Combine(directory,"mask.png"));var cut=(byte[])workspace.Original.Pixels.Clone();for(int i=0;i<combined.Length;i++)cut[i*4+3]=(byte)(cut[i*4+3]*(255-combined[i])/255);new Raster(workspace.Original.W,workspace.Original.H,cut).Save(Path.Combine(directory,"cutout.png"));
  var first=layers.FirstOrDefault(l=>l.Flow!=null);first?.Flow?.Save(Path.Combine(directory,"flow.png"));
  var settings=new FlowSettings{schemaVersion=2,activeLayer=export?0:workspace.ActiveLayer,layers=configs.ToArray(),shader=workspace.Shader};
  File.WriteAllText(Path.Combine(directory,"session.json"),JsonSerializer.Serialize(settings,new JsonSerializerOptions{IncludeFields=true,WriteIndented=true}));return settings;
 }
 static string Child(string directory,string file)
 {
  if(string.IsNullOrEmpty(file)||Path.IsPathRooted(file))throw new IOException("Invalid area texture path.");string full=Path.GetFullPath(Path.Combine(directory,file));string root=Path.GetFullPath(directory)+Path.DirectorySeparatorChar;if(!full.StartsWith(root,StringComparison.OrdinalIgnoreCase))throw new IOException("Area textures must remain inside the saved work folder.");return full;
 }
 public static WorkspaceSnapshot Read(string directory)
 {
  var options=new JsonSerializerOptions{IncludeFields=true};var settings=JsonSerializer.Deserialize<FlowSettings>(File.ReadAllText(Path.Combine(directory,"session.json")),options)??throw new IOException("Missing session settings.");
  if(settings.schemaVersion!=1&&settings.schemaVersion!=2)throw new IOException("Unsupported saved-work format.");
  var original=Raster.Load(Path.Combine(directory,"base.png"));
  LayerSnapshot Load(string name,bool enabled,string maskFile,string flowFile,MotionSettings motion,string displayName="Saved moving texture"){ValidateMotion(motion);var maskRaster=Raster.Load(Child(directory,maskFile));if(maskRaster.W!=original.W||maskRaster.H!=original.H)throw new IOException("Area mask dimensions do not match the original.");var mask=new byte[original.W*original.H];for(int i=0;i<mask.Length;i++)mask[i]=maskRaster.Pixels[i*4];return new(name,enabled,mask,string.IsNullOrEmpty(flowFile)?null:Raster.Load(Child(directory,flowFile)),motion,string.IsNullOrEmpty(flowFile)?"No moving texture yet":(string.IsNullOrEmpty(displayName)?"Saved moving texture":displayName));}
  if(settings.schemaVersion==2&&(settings.layers==null||settings.layers.Length<1||settings.layers.Length>MainWindow.MaxEffects))throw new IOException("Unsupported number of saved areas.");
  var layers=settings.schemaVersion==1?new[]{Load("Area 1",true,"mask.png","flow.png",settings)}:settings.layers.Select(l=>Load(l.name,l.enabled,l.maskFile,l.flowFile,l.settings,l.flowName)).ToArray();
  if(layers.Length<1||layers.Length>MainWindow.MaxEffects)throw new IOException("Unsupported number of saved areas.");
  return new(original,layers,Math.Clamp(settings.activeLayer,0,layers.Length-1),settings.shader);
 }
}

public partial class MainWindow
{
 WorkspaceSnapshot CaptureWorkspace(bool validateExport)
 {
  Check();SaveActiveEffect();var layers=EffectLayers.Select(l=>new LayerSnapshot(l.Name,l.Enabled,l.Mask,l.Flow,CopySettings(l.Settings),l.FlowName)).ToArray();var workspace=new WorkspaceSnapshot(image!,layers,Math.Max(0,EffectLayers.IndexOf(activeEffect!)),Settings().shader);if(validateExport)WorkspaceFiles.ReadyLayers(workspace);return workspace;
 }
 public async Task SaveWorkspaceAsync(string file)
 {
  var workspace=CaptureWorkspace(false);
  await Task.Run(()=>{string directory=file+".assets";WorkspaceFiles.Write(directory,workspace,false);File.WriteAllText(file,JsonSerializer.Serialize(new{assets=Path.GetFileName(directory)}));});
 }
 public async Task LoadWorkspaceAsync(string file)
 {
  string root=Path.GetDirectoryName(Path.GetFullPath(file))!;using var json=JsonDocument.Parse(File.ReadAllText(file));string directory=Path.GetFullPath(Path.Combine(root,json.RootElement.GetProperty("assets").GetString()!));if(!directory.StartsWith(root+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new IOException("Saved textures must remain beside the work file.");
  var workspace=await Task.Run(()=>WorkspaceFiles.Read(directory));
  SetImage(workspace.Original);changingEffects=true;EffectLayers.Clear();foreach(var l in workspace.Layers)EffectLayers.Add(new EffectLayer{Name=l.Name,Enabled=l.Enabled,Mask=l.Mask,Flow=l.Flow,Settings=l.Settings,FlowName=l.FlowName});nextArea=EffectLayers.Count+1;var selected=EffectLayers[workspace.ActiveLayer];Effects.SelectedItem=selected;changingEffects=false;ActivateEffect(selected);Steps.SelectedIndex=1;Status.Text=$"Opened saved work with {EffectLayers.Count} independent areas.";
 }
}
