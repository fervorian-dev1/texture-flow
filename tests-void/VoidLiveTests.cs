using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using TextureFlow;
static class VoidLiveTests {
 static T Field<T>(object o,string name)=>(T)o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(o)!;
 static async Task Wait(Func<bool> test,int seconds){var end=DateTime.UtcNow.AddSeconds(seconds);while(!test()){if(DateTime.UtcNow>end)throw new Exception("Live wait timed out");await Task.Delay(200);}}
 public static int Run(string root){var app=new Application{ShutdownMode=ShutdownMode.OnExplicitShutdown};int code=0;app.Startup+=async(_,_)=>{MainWindow? owner=null;SkyboxVoidTab? tab=null;try{owner=new MainWindow();owner.Show();var outer=(TabControl)owner.Content;outer.SelectedIndex=1;tab=(SkyboxVoidTab)((TabItem)outer.Items[1]).Content;tab.LoadExample();tab.Tools.VoidProject=root;await Wait(()=>File.Exists(root+"/Library/TextureFlow/live-ready.txt"),70);await tab.Tools.VoidConnectAsync();tab.SetActive(true);var targets=Field<ComboBox>(tab,"Targets");await Wait(()=>targets.Items.Count>0,15);var chosen=targets.Items.Cast<AvatarTarget>().First(t=>t.label.Contains("Disposable void test")&&t.slot==0);string guid=chosen.materialGuid;targets.SelectedItem=chosen;await tab.ApplyVoidAsync();Check(File.Exists(root+"/Library/TextureFlow/Void/desktop-restore.json"),"desktop apply confirmed with own receipt");await tab.RestoreVoidAsync();await Wait(()=>tab.Tools.VoidConnection()?.targets.Any(t=>t.key==chosen.key&&t.materialGuid==guid)==true,12);Check(true,"desktop restore returned original material GUID");File.WriteAllText(root+"/live-apply-result.txt","PASS live Skybox/Void desktop apply and restore; original material GUID restored.");}catch(Exception ex){Console.Error.WriteLine(ex);File.WriteAllText(root+"/live-apply-result.txt",ex.ToString());code=1;}finally{Directory.CreateDirectory(root+"/Library/TextureFlow");File.WriteAllText(root+"/Library/TextureFlow/live-stop.txt","stop");tab?.Dispose();owner?.Close();app.Shutdown();}};app.Run();return code;}
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);Console.WriteLine("PASS "+text);}
}
