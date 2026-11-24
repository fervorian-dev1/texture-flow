using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using ImageMagick;
using TextureFlow;
static class ImportTests
{
 static readonly BindingFlags Flags=BindingFlags.Instance|BindingFlags.NonPublic|BindingFlags.DeclaredOnly;
 static T Field<T>(object o,string n)=>(T)o.GetType().GetField(n,Flags)!.GetValue(o)!;
 public static async Task Run(string root,MainWindow main,Action<bool,string> check)
 {
  string folder=Path.Combine(root,"Assets","Browser Fixtures");Directory.CreateDirectory(folder);
  string png=Path.Combine(folder,"Sammy shoes stripe.png");
  new Raster(2,2,new byte[]{0,0,255,255,0,255,0,255,255,0,0,255,255,255,255,255}).Save(png);
  foreach(var pair in new[]{("stripe.tga",MagickFormat.Tga),("stripe.psd",MagickFormat.Psd),("stripe.dds",MagickFormat.Dds),("stripe.exr",MagickFormat.Exr)})
  {using var im=new MagickImage(png);im.Write(Path.Combine(folder,pair.Item1),pair.Item2);}
  var found=await Task.Run(()=>ProjectTextures.Scan(root,false,CancellationToken.None));
  check(found.Count(e=>e.RelativePath.Contains("Browser Fixtures"))==5,"browser discovers PNG TGA PSD DDS EXR");
  foreach(var file in Directory.GetFiles(folder).Where(ProjectTextures.IsTexture))
  {var raster=await Task.Run(()=>Raster.Load(file));var thumb=await Task.Run(()=>ProjectTextures.Thumbnail(file));check(raster.W==2&&raster.H==2&&thumb.PixelWidth>0,Path.GetExtension(file)+" image and thumbnail decode");}
  var data=new DataObject(DataFormats.FileDrop,new[]{png});
  check(ProjectTextures.DropPaths(data,null).Single()==png,"Windows file-drop payload");
  var uri=new DataObject(DataFormats.UnicodeText,new Uri(png).AbsoluteUri);
  check(ProjectTextures.DropPaths(uri,null).Single()==png,"URI drop handles spaces");
  var relative=new DataObject(DataFormats.UnicodeText,"Assets/Browser Fixtures/Sammy shoes stripe.png");
  check(ProjectTextures.DropPaths(relative,root).Single()==png,"Unity relative asset path drop");
  await main.ImportDropAsync(new DataObject(DataFormats.FileDrop,new[]{root}),false);
  check(Field<ComboBox>(main,"Projects").SelectedItem as string==root,"project folder drop selects project");
  await main.ImportDropAsync(data,false);check(Field<Raster>(main,"image").W==2,"dropped base image loads");
  await main.ImportDropAsync(new DataObject(DataFormats.FileDrop,new[]{Path.Combine(folder,"stripe.tga")}),true);
  check(Field<Raster>(main,"flow").W==2,"animation drop target loads layer");
  string bad=Path.Combine(folder,"broken.png");File.WriteAllText(bad,"invalid image");var original=Field<Raster>(main,"image");bool failed=false;
  try{await main.ImportDropAsync(new DataObject(DataFormats.FileDrop,new[]{png,bad}),false);}catch{failed=true;}
  check(failed&&ReferenceEquals(original,Field<Raster>(main,"image")),"two-image drop failure preserves existing layers");
  File.Delete(bad);
  using var cts=new CancellationTokenSource();cts.Cancel();bool cancelled=false;try{ProjectTextures.Scan(root,false,cts.Token);}catch(OperationCanceledException){cancelled=true;}
  check(cancelled,"project scan cancellation");
  var browser=new TextureBrowser(root){ShowInTaskbar=false,Opacity=0};browser.Show();
  var list=Field<ListBox>(browser,"Results");var until=DateTime.UtcNow.AddSeconds(15);
  while(list.ItemsSource==null&&DateTime.UtcNow<until)await Task.Delay(50);
  check(list.Items.Count>=5,"texture browser async results populate");
  Field<TextBox>(browser,"Search").Text="Sammy stripe";
  check(list.Items.Count==1,"browser search combines filename terms");
  list.SelectedIndex=0;await Task.Delay(500);
  check(Field<Button>(browser,"BaseButton").IsEnabled&&((TextureEntry)list.SelectedItem).Thumbnail!=null,"thumbnail and layer buttons ready for selected image");
  var content=(FrameworkElement)browser.Content;content.UpdateLayout();var shot=new System.Windows.Media.Imaging.RenderTargetBitmap((int)content.ActualWidth,(int)content.ActualHeight,96,96,System.Windows.Media.PixelFormats.Pbgra32);shot.Render(content);var enc=new System.Windows.Media.Imaging.PngBitmapEncoder();enc.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(shot));using(var f=File.Create(Path.Combine(root,"browser-preview.png")))enc.Save(f);
  check(main.Icon!=null&&browser.Icon!=null,"attached icon embedded in both windows");browser.Close();
 }
}

