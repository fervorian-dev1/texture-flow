using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using TextureFlow;
static class LayerTests
{
 static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.DeclaredOnly;
 static T Field<T>(object o,string n)=>(T)o.GetType().GetField(n,Flags)!.GetValue(o)!;
 static object? Call(object o,string n,params object?[] args)=>o.GetType().GetMethod(n,Flags)!.Invoke(o,args);
 static Raster Solid(byte b,byte g,byte r,int size=4){var p=new byte[size*size*4];for(int i=0;i<size*size;i++){p[i*4]=b;p[i*4+1]=g;p[i*4+2]=r;p[i*4+3]=255;}return new(size,size,p);}
 public static async Task Run(string root,MainWindow main,Action<bool,string> check)
 {
  Call(main,"SetImage",Solid(0,0,0));Call(main,"SetMovingTexture",Solid(0,0,255),"Red moving image");Field<TextBox>(main,"Emission").Text="0";
  main.MergeSelection(new byte[]{255,0,0,0,255,0,0,0,255,0,0,0,255,0,0,0},SelectionOperation.Replace);
  var original=(byte[])Field<byte[]>(main,"mask").Clone();Call(main,"QuickSelectionTool",new Button{Tag="3"},new RoutedEventArgs());Call(main,"Remember");Call(main,"Paint",2d,2d);
  check(original.Where((v,i)=>v>0).All(v=>v==255)&&Field<byte[]>(main,"mask")[0]==255,"fill then brush keeps existing selected pixels");
  Call(main,"Undo",null,new RoutedEventArgs());check(Field<byte[]>(main,"mask").SequenceEqual(original),"brush undo restores filled region");Call(main,"RedoSelection",null,new RoutedEventArgs());check(!Field<byte[]>(main,"mask").SequenceEqual(original),"brush redo restores refinement");
  main.MergeSelection(original,SelectionOperation.Replace);var first=main.EffectLayers[0];first.Name="Red stripe";Field<TextBox>(main,"Speed").Text="0.1";
  var second=main.AddEffectLayer();second.Name="Green sole";Call(main,"SetMovingTexture",Solid(0,255,0),"Green moving image");Field<TextBox>(main,"Speed").Text="0.7";Field<System.Windows.Controls.Slider>(main,"Angle").Value=180;Field<TextBox>(main,"Emission").Text="0";
  main.MergeSelection(new byte[]{0,0,0,255,0,0,0,255,0,0,0,255,0,0,0,255},SelectionOperation.Replace);
  check(main.EffectLayers.Count==2&&!ReferenceEquals(first.Mask,second.Mask),"areas keep separate mask arrays");
  Field<ListBox>(main,"Effects").SelectedItem=first;
  check(Field<TextBox>(main,"Speed").Text=="0.1"&&ReferenceEquals(Field<Raster>(main,"flow"),first.Flow)&&Field<byte[]>(main,"mask").SequenceEqual(original),"switching back restores first image, mask and settings");
  Field<ListBox>(main,"Effects").SelectedItem=second;check(Field<TextBox>(main,"Speed").Text=="0.7"&&Field<Slider>(main,"Angle").Value==180,"second area's independent speed and direction restored");
  check(Field<TextBox>(main,"Tint").Text=="#FFFFFF","new areas use neutral white tint");
  var patch=new byte[16];patch[5]=255;main.MergeSelection(patch,SelectionOperation.Add);check(Field<byte[]>(main,"mask")[5]==255&&Field<byte[]>(main,"mask")[3]==255,"adding a filled patch expands current area");main.MergeSelection(patch,SelectionOperation.Subtract);check(Field<byte[]>(main,"mask")[5]==0&&Field<byte[]>(main,"mask")[3]==255,"subtracting removes only requested patch");
  Call(main,"SaveActiveEffect");
  var render=LayerCompositor.Render(Solid(0,0,0),new[]{new EffectFrame(first.Flow!,first.Mask,first.Settings),new EffectFrame(second.Flow!,second.Mask,second.Settings)},0,showGlow:false,maxWidth:4);
  check(render.Pixels[2]==255&&render.Pixels[3*4+1]==255&&render.Pixels[1*4]==0&&render.Pixels[1*4+1]==0&&render.Pixels[1*4+2]==0,"composite shows independent colors and untouched original region");
  var overlap=Enumerable.Repeat((byte)255,16).ToArray();var both=LayerCompositor.Render(Solid(0,0,0),new[]{new EffectFrame(first.Flow!,overlap,first.Settings),new EffectFrame(second.Flow!,overlap,second.Settings)},0,showGlow:false,maxWidth:4);check(both.Pixels[1]==255&&both.Pixels[2]==0,"later area covers earlier area in overlapping masks");
  var patterned=new Raster(2,2,new byte[]{0,0,255,255,0,255,0,255,255,0,0,255,255,255,255,255});var moving=new MotionSettings{speed=.25f,angle=0,emission=0};var a=LayerCompositor.Render(Solid(0,0,0),new[]{new EffectFrame(patterned,overlap,moving)},0,maxWidth:4);var b=LayerCompositor.Render(Solid(0,0,0),new[]{new EffectFrame(patterned,overlap,moving)},2,maxWidth:4);check(!a.Pixels.SequenceEqual(b.Pixels),"preview pattern changes with independent motion time");
  string file=Path.Combine(root,"two-area-test.tflow");await main.SaveWorkspaceAsync(file);main.AddEffectLayer();await main.LoadWorkspaceAsync(file);
  check(main.EffectLayers.Count==2&&main.EffectLayers[0].Name=="Red stripe"&&main.EffectLayers[1].Name=="Green sole","saved work restores all named areas");check(main.EffectLayers[0].Settings.speed==.1f&&main.EffectLayers[1].Settings.speed==.7f,"saved work preserves independent motion settings");check(main.EffectLayers[0].Mask.SequenceEqual(original)&&main.EffectLayers[0].Flow!.Pixels[2]==255&&main.EffectLayers[1].Flow!.Pixels[1]==255,"saved work preserves independent image pixels and masks");
  var duplicate=main.AddEffectLayer(true);check(!ReferenceEquals(duplicate.Mask,main.EffectLayers[1].Mask)&&duplicate.Mask.SequenceEqual(main.EffectLayers[1].Mask),"duplicated area gets an independent mask copy");Call(main,"RemoveArea",null,new RoutedEventArgs());check(main.EffectLayers.Count==2,"remove affects only one area");Call(main,"RestoreRemovedArea",null,new RoutedEventArgs());check(main.EffectLayers.Count==3,"removed area can be restored");Call(main,"RemoveArea",null,new RoutedEventArgs());
  string export=await main.ExportAssetsAsync(root,new FlowSettings());var config=JsonSerializer.Deserialize<FlowSettings>(File.ReadAllText(Path.Combine(export,"flow-request.json")),new JsonSerializerOptions{IncludeFields=true})!;
  check(config.schemaVersion==2&&config.layers.Length==2&&config.layers[0].settings.speed!=config.layers[1].settings.speed,"Unity request carries both independent areas and settings");
  check(File.Exists(Path.Combine(export,config.layers[0].flowFile))&&File.Exists(Path.Combine(export,config.layers[1].maskFile)),"export contains separate area images and masks");
  var union=Raster.Load(Path.Combine(export,"mask.png"));check(union.Pixels[0]==255&&union.Pixels[3*4]==255&&union.Pixels[1*4]==0,"combined cutout mask includes both areas only");
  main.EffectLayers[1].Enabled=false;string filtered=await main.ExportAssetsAsync(root,new FlowSettings());var enabled=JsonSerializer.Deserialize<FlowSettings>(File.ReadAllText(Path.Combine(filtered,"flow-request.json")),new JsonSerializerOptions{IncludeFields=true})!;check(enabled.layers.Length==1,"disabled areas are excluded from Unity material export");main.EffectLayers[1].Enabled=true;
  var empty=main.AddEffectLayer();string withDraft=await main.ExportAssetsAsync(root,new FlowSettings());check(JsonSerializer.Deserialize<FlowSettings>(File.ReadAllText(Path.Combine(withDraft,"flow-request.json")),new JsonSerializerOptions{IncludeFields=true})!.layers.Length==2,"untouched empty draft does not block completed areas");
  while(main.EffectLayers.Count<MainWindow.MaxEffects)main.AddEffectLayer();bool limited=false;try{main.AddEffectLayer();}catch(IOException){limited=true;}check(limited&&main.EffectLayers.Count==8,"eight-area limit is explicit and preserves existing work");
  string legacy=Path.Combine(root,"legacy-work");Directory.CreateDirectory(legacy);Solid(0,0,0).Save(Path.Combine(legacy,"base.png"));Solid(0,0,255).Save(Path.Combine(legacy,"flow.png"));Solid(255,255,255).Save(Path.Combine(legacy,"mask.png"));File.WriteAllText(Path.Combine(legacy,"session.json"),JsonSerializer.Serialize(new FlowSettings{schemaVersion=1,tint="#40DFFF"},new JsonSerializerOptions{IncludeFields=true}));var old=WorkspaceFiles.Read(legacy);check(old.Layers.Length==1&&old.Layers[0].Settings.tint=="#40DFFF","legacy single-area work loads without altering its original tint");
  await main.LoadWorkspaceAsync(file);Field<ListBox>(main,"Effects").SelectedIndex=0;Field<TextBox>(main,"Speed").Text="0.1";Field<Slider>(main,"Angle").Value=90;Call(main,"SaveActiveEffect");
  // Leave the latest export with two areas for Unity tests, while keeping the first area's historic expected velocity.
  await main.ExportAssetsAsync(root,new FlowSettings());
 }
}
