using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using TextureFlow;
class Tests {
 static int passed;
 static void Assert(bool ok,string name){if(!ok)throw new Exception(name);Console.WriteLine("PASS "+name);passed++;}
 static readonly BindingFlags Flags=BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.DeclaredOnly;
 static T Field<T>(object o,string name)=>(T)o.GetType().GetField(name,Flags)!.GetValue(o)!;
 static object? Call(object o,string name,params object?[] args)=>o.GetType().GetMethod(name,Flags)!.Invoke(o,args);
 [STAThread] static int Main(string[] args){if(args.Length>1&&args[1]=="--live")return LiveApplyTests.Run(Path.GetFullPath(args[0]));var app=new Application();int exit=0;app.Startup+=async(_,_)=>{try{
 byte[] p=new byte[5*5*4];for(int i=0;i<25;i++){p[i*4+3]=255;p[i*4]=p[i*4+1]=p[i*4+2]=200;}foreach(int i in new[]{0,1,5,24})p[i*4]=p[i*4+1]=p[i*4+2]=10;var im=new Raster(5,5,p);
 Assert(Selection.Select(im,0,0,0,true,false,0,0,0).Count(v=>v==255)==3,"wand preserves disconnected region");Assert(Selection.Select(im,0,0,0,false,false,0,0,0).Count(v=>v==255)==4,"color finds disconnected regions");
 var mask=new byte[25];mask[12]=255;Assert(Selection.Filter(mask,5,5,1,"max").Count(v=>v>0)==9,"grow 3x3");Assert(Selection.Filter(Selection.Filter(mask,5,5,1,"max"),5,5,1,"min").SequenceEqual(mask),"shrink restores center");Assert(Selection.Filter(mask,5,5,1,"blur")[12]==28,"feather soft alpha");
 var ring=Enumerable.Repeat((byte)255,25).ToArray();ring[12]=0;ring[0]=0;var holes=Selection.Holes(ring,5,5);Assert(holes[12]==255&&holes[0]==0,"hole fill preserves exterior");
 var poly=Selection.Polygon(5,5,new(){(1,1),(4,1),(4,4),(1,4)});Assert(poly.Count(v=>v>0)==9,"polygon scanline coverage");Selection.Brush(mask,5,5,2,2,2,0,true);Assert(mask[12]==0,"subtract brush");
 var rng=new Random(8);var input=new byte[31*23];rng.NextBytes(input);foreach(string mode in new[]{"max","min"}){var result=Selection.Filter(input,31,23,4,mode);for(int y=0;y<23;y++)for(int x=0;x<31;x++){var list=new List<byte>();for(int yy=Math.Max(0,y-4);yy<=Math.Min(22,y+4);yy++)for(int xx=Math.Max(0,x-4);xx<=Math.Min(30,x+4);xx++)list.Add(input[yy*31+xx]);AssertQuiet(result[y*31+x]==(mode=="max"?list.Max():list.Min()),"morphology reference mismatch");}Assert(true,mode+" matches naive random reference");}
 var hsv=Selection.Hsv(0,0,255);Assert(hsv.h==0&&hsv.s==1&&hsv.v==1,"HSV red conversion");
 string root=Path.GetFullPath(args.Length>0?args[0]:"test-export");Directory.CreateDirectory(Path.Combine(root,"Assets"));Directory.CreateDirectory(Path.Combine(root,"ProjectSettings"));
 var window=new MainWindow();Call(window,"Demo",null,new RoutedEventArgs());var demo=Field<Raster>(window,"image");Call(window,"Render");Assert(Field<System.Windows.Controls.Image>(window,"Preview").Source!=null,"WPF demo preview renders");var content=(FrameworkElement)window.Content;content.Measure(new Size(1280,820));content.Arrange(new Rect(0,0,1280,820));content.UpdateLayout();var shot=new System.Windows.Media.Imaging.RenderTargetBitmap(1280,820,96,96,System.Windows.Media.PixelFormats.Pbgra32);shot.Render(content);var encoder=new System.Windows.Media.Imaging.PngBitmapEncoder();encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(shot));using(var file=File.Create(Path.Combine(root,"desktop-preview.png")))encoder.Save(file);var selection=Selection.Select(demo,256,256,0,true,false,0,0,0);typeof(MainWindow).GetField("mask",Flags)!.SetValue(window,selection);var projects=Field<ComboBox>(window,"Projects");projects.Items.Add(root);projects.SelectedItem=root;Call(window,"Export",null,new RoutedEventArgs());while(Field<bool>(window,"busy"))await Task.Delay(50);var folders=Directory.GetDirectories(Path.Combine(root,"Assets/TextureFlow/Exports"));string export=folders.Last();Assert(File.Exists(Path.Combine(export,"flow-request.json")),"Unity export committed");var cut=Raster.Load(Path.Combine(export,"cutout.png"));var exportedMask=Raster.Load(Path.Combine(export,"mask.png"));Assert(cut.Pixels[(256*512+256)*4+3]==0&&exportedMask.Pixels[(256*512+256)*4]==255,"cutout alpha and mask polarity");Assert(demo.Pixels[(256*512+256)*4+3]==255,"original remains intact");
 await ImportTests.Run(root,window,Assert);
 await GuidedTests.Run(root,window,Assert);
 await LayerTests.Run(root,window,Assert);
 var large=new byte[8192*8192];large[4096*8192+4096]=255;var sw=System.Diagnostics.Stopwatch.StartNew();var grown=await Task.Run(()=>Selection.Filter(large,8192,8192,8,"max"));Assert(grown.Count(v=>v==255)==17*17,"8192² grow selection");Console.WriteLine("8192² grow: "+sw.ElapsedMilliseconds+" ms");File.WriteAllText(Path.Combine(root,"desktop-tests.txt"),passed+" checks passed; 8192² grow "+sw.ElapsedMilliseconds+" ms");Console.WriteLine(passed+" checks passed.");window.Close();
 }catch(Exception ex){Console.Error.WriteLine(ex);exit=1;}finally{app.Shutdown();}};app.Run();return exit;}
 static void AssertQuiet(bool ok,string name){if(!ok)throw new Exception(name);}
}


