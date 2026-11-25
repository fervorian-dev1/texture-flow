using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using TextureFlow;
static class LiveApplyTests
{
 static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.DeclaredOnly;
 static T Field<T>(object o,string n)=>(T)o.GetType().GetField(n,Flags)!.GetValue(o)!;
 static void Call(object o,string method,params object?[] args)=>o.GetType().GetMethod(method,Flags)!.Invoke(o,args);
 static async Task Wait(Func<bool> done,int seconds,string message){var end=DateTime.UtcNow.AddSeconds(seconds);while(!done()){if(DateTime.UtcNow>end)throw new Exception(message);await Task.Delay(100);}}
 public static int Run(string root)
 {
  int code=0;var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};
  app.Startup+=async(_,_)=>{MainWindow? main=null;try{
   main=new MainWindow();Call(main,"Demo",null,new RoutedEventArgs());var projects=Field<ComboBox>(main,"Projects");projects.Items.Add(root);projects.SelectedItem=root;
   await Wait(()=>{Call(main,"RefreshConnection");return Field<ComboBox>(main,"Targets").Items.Count>0;},45,"Live Unity connector did not appear.");
   var targets=Field<ComboBox>(main,"Targets");var target=targets.Items.Cast<AvatarTarget>().Single(t=>t.label.Contains("Beginner test avatar / Shoes")&&t.slot==0);string originalGuid=target.materialGuid;targets.SelectedItem=target;
   Call(main,"CreateAndApply",null,new RoutedEventArgs());await Wait(()=>!Field<bool>(main,"busy"),100,"Create & Apply did not finish.");
   string? receipt=Field<string?>(main,"lastReceipt");if(receipt==null||!File.Exists(Path.Combine(root,receipt)))throw new Exception("Apply button did not produce a receipt: "+Field<TextBlock>(main,"Status").Text);
   var config=System.Text.Json.JsonSerializer.Deserialize<FlowSettings>(File.ReadAllText(Path.Combine(root,Path.GetDirectoryName(receipt)!,"flow-request.json")),new System.Text.Json.JsonSerializerOptions{IncludeFields=true})!;if(config.schemaVersion!=2||config.layers.Length!=2||config.layers[0].settings.speed==config.layers[1].settings.speed)throw new Exception("Live apply did not send both independent areas.");
   Console.WriteLine("PASS actual desktop Create & Apply button sent two independent areas and received Unity confirmation");
   if(!Field<Button>(main,"RestoreButton").IsEnabled)throw new Exception("Restore action was not enabled.");
   Call(main,"RestoreMaterial",null,new RoutedEventArgs());await Wait(()=>!Field<bool>(main,"busy"),60,"Restore did not finish.");
   if(!Field<TextBlock>(main,"Status").Text.StartsWith("Previous material restored"))throw new Exception("Restore failed: "+Field<TextBlock>(main,"Status").Text);
   await Wait(()=>{Call(main,"RefreshConnection");return Field<ComboBox>(main,"Targets").Items.Cast<AvatarTarget>().Any(t=>t.key==target.key&&t.materialGuid==originalGuid);},10,"Unity did not restore the original material GUID.");
   Console.WriteLine("PASS actual desktop Restore button restored original material in running Unity");File.WriteAllText(Path.Combine(root,"live-apply-result.txt"),"PASS desktop Create & Apply with two independently animated areas, and Restore through a live Unity editor; original material GUID restored.");
  }catch(Exception ex){Console.Error.WriteLine(ex);File.WriteAllText(Path.Combine(root,"live-apply-result.txt"),ex.ToString());code=1;}finally{File.WriteAllText(Path.Combine(root,"Library/TextureFlow/live-stop.txt"),"stop");main?.Close();app.Shutdown();}};app.Run();return code;
 }
}
