using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Windows.Controls;
using TextureFlow;
static class GuidedTests
{
 static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.DeclaredOnly;
 static T Field<T>(object o,string name)=>(T)o.GetType().GetField(name,Flags)!.GetValue(o)!;
 static void Refresh(MainWindow main)=>main.GetType().GetMethod("RefreshConnection",Flags)!.Invoke(main,null);
 public static async Task Run(string root,MainWindow main,Action<bool,string> check)
 {
  check(Field<TabControl>(main,"Steps").Items.Count==3,"three-step beginner interface");
  check(Field<CheckBox>(main,"ShowMask").IsChecked==false,"clean animation preview by default");
  check(Field<Button>(main,"ApplyButton").Content.ToString()=="Create & Apply","plain-language automatic apply action");
  string bridge=Path.Combine(root,"Library/TextureFlow");Directory.CreateDirectory(bridge);
  string path=Path.Combine(bridge,"connection.json");
  var targets=new[]{new AvatarTarget{key="part-1",instanceId=123,slot=0,materialInstanceId=42,label="Avatar / Shoes | Shoe material (slot 0)",texturePath="Assets/Browser Fixtures/Sammy shoes stripe.png"}};
  var state=new UnityConnection{protocolVersion=1,sessionId="test-session",generatedUtc=DateTime.UtcNow.ToString("O"),message="Connected",targets=targets};
  File.WriteAllText(path,JsonSerializer.Serialize(state));Refresh(main);
  check(Field<ComboBox>(main,"Targets").Items.Count==1,"connector target list loads");
  check((Field<ComboBox>(main,"Targets").SelectedItem as AvatarTarget)?.key=="part-1","unique texture match selects correct avatar part");
  Field<ComboBox>(main,"Targets").SelectedIndex=-1;
  state.targets=new[]{targets[0],new AvatarTarget{key="part-2",instanceId=124,slot=0,label="Second avatar / Shoes",texturePath=targets[0].texturePath}};
  File.WriteAllText(path,JsonSerializer.Serialize(state));Refresh(main);
  check(Field<ComboBox>(main,"Targets").SelectedIndex==-1,"ambiguous avatar targets require a choice");
  Field<ComboBox>(main,"Targets").SelectedIndex=1;state.targets[1].label+=" renamed";File.WriteAllText(path,JsonSerializer.Serialize(state));Refresh(main);
  check((Field<ComboBox>(main,"Targets").SelectedItem as AvatarTarget)?.key=="part-2","manual part choice preserved across refresh");
  File.SetLastWriteTimeUtc(path,DateTime.UtcNow.AddMinutes(-1));Refresh(main);
  check(Field<ComboBox>(main,"Targets").Items.Count==0,"stale Unity heartbeat clears selectable targets");
  File.WriteAllText(path,"partially written JSON");Refresh(main);
  check(Field<ComboBox>(main,"Targets").Items.Count==0,"transient connector read failure is handled");
  var command=new UnityCommand{sessionId="test-session",target=targets[0]};
  check(DateTime.Parse(command.expiresUtc).ToUniversalTime()>DateTime.UtcNow,"apply commands have an expiry");
  var completion=new TaskCompletionSource();
  var work=(Task)main.GetType().GetMethod("Work",Flags)!.Invoke(main,new object[]{(Func<Task>)(()=>completion.Task)})!;
  check(!Field<Button>(main,"ApplyButton").IsEnabled&&!Field<ComboBox>(main,"Projects").IsEnabled,"project and apply action disabled during work");completion.SetResult();await work;
  check(Field<Button>(main,"ApplyButton").IsEnabled&&Field<ComboBox>(main,"Projects").IsEnabled,"controls restored after work completes");
  File.Delete(path);
 }
}
