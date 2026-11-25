using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;
namespace TextureFlow;
public sealed class Raster {
 public int W {get;} public int H {get;} public byte[] Pixels {get;}
 public Raster(int w,int h,byte[] p){W=w;H=h;Pixels=p;}
 public static Raster Load(string path){
  using var info=new ImageMagick.MagickImage();info.Ping(path);
  if(info.Width>8192||info.Height>8192)throw new Exception("Maximum texture size is 8192 ? 8192.");
  using var decoded=new ImageMagick.MagickImage();decoded.Read(path,new ImageMagick.MagickReadSettings{FrameIndex=0,FrameCount=1});
  decoded.AutoOrient();using var stream=new MemoryStream(decoded.ToByteArray(ImageMagick.MagickFormat.Png));
  var b=BitmapFrame.Create(stream,BitmapCreateOptions.PreservePixelFormat,BitmapCacheOption.OnLoad);
  var c=new FormatConvertedBitmap(b,PixelFormats.Bgra32,null,0);var p=new byte[c.PixelWidth*c.PixelHeight*4];c.CopyPixels(p,c.PixelWidth*4,0);return new(c.PixelWidth,c.PixelHeight,p);
 }

 public BitmapSource Bitmap(){var b=BitmapSource.Create(W,H,96,96,PixelFormats.Bgra32,null,Pixels,W*4);b.Freeze();return b;}
 public void Save(string path){var e=new PngBitmapEncoder();e.Frames.Add(BitmapFrame.Create(Bitmap()));using var f=File.Create(path);e.Save(f);}
}
public enum SelectionOperation { Replace, Add, Subtract }
public static class Selection {
 public static byte[] Combine(byte[] current,byte[] incoming,SelectionOperation operation){if(current.Length!=incoming.Length)throw new ArgumentException("Selection dimensions differ.");if(operation==SelectionOperation.Replace)return incoming;for(int i=0;i<current.Length;i++)current[i]=operation==SelectionOperation.Add?Math.Max(current[i],incoming[i]):(byte)Math.Min(current[i],255-incoming[i]);return current;}

 public static (double h,double s,double v) Hsv(byte b,byte g,byte r){double max=Math.Max(r,Math.Max(g,b)),min=Math.Min(r,Math.Min(g,b)),d=max-min;double h=d==0?0:max==r?60*((g-b)/d%6):max==g?60*((b-r)/d+2):60*((r-g)/d+4);return ((h+360)%360,max==0?0:d/max,max/255);}
 public static byte[] Select(Raster im,int x,int y,double tolerance,bool connected,bool hsv,double ht,double st,double vt){int n=im.W*im.H,seed=y*im.W+x;var result=new byte[n];var p=im.Pixels;var target=Hsv(p[seed*4],p[seed*4+1],p[seed*4+2]);
 bool Matches(int i){i*=4;if(!hsv)return Math.Max(Math.Abs(p[i]-p[seed*4]),Math.Max(Math.Abs(p[i+1]-p[seed*4+1]),Math.Abs(p[i+2]-p[seed*4+2])))<=tolerance;var c=Hsv(p[i],p[i+1],p[i+2]);double dh=Math.Abs(c.h-target.h);return Math.Min(dh,360-dh)<=ht&&Math.Abs(c.s-target.s)<=st&&Math.Abs(c.v-target.v)<=vt;}
 if(!connected){Parallel.For(0,n,i=>{if(Matches(i))result[i]=255;});return result;}
 var seen=new System.Collections.BitArray(n);var q=new Queue<int>();q.Enqueue(seed);seen[seed]=true;while(q.Count>0){int i=q.Dequeue();if(!Matches(i))continue;result[i]=255;void Visit(int j){if(!seen[j]){seen[j]=true;q.Enqueue(j);}}if(i%im.W>0)Visit(i-1);if(i%im.W<im.W-1)Visit(i+1);if(i>=im.W)Visit(i-im.W);if(i<n-im.W)Visit(i+im.W);}return result;
 }
 public static byte[] Filter(byte[] src,int w,int h,int radius,string mode){radius=Math.Clamp(radius,1,128);var tmp=new byte[src.Length];var dst=new byte[src.Length];
 void Pass(byte[] input,byte[] output,int lines,int length,bool vertical){Parallel.For(0,lines,line=>{int At(int v)=>vertical?v*w+line:line*w+v;if(mode=="blur"){long sum=0;int right=-1,left=0;for(int i=0;i<length;i++){int end=Math.Min(length-1,i+radius);while(right<end)sum+=input[At(++right)];int start=Math.Max(0,i-radius);while(left<start)sum-=input[At(left++)];output[At(i)]=(byte)(sum/(right-left+1));}}else{var deque=new int[length];int head=0,tail=0,next=0;for(int i=0;i<length;i++){while(next<=Math.Min(length-1,i+radius)){byte v=input[At(next)];while(tail>head&&(mode=="max"?input[At(deque[tail-1])]<=v:input[At(deque[tail-1])]>=v))tail--;deque[tail++]=next++;}while(head<tail&&deque[head]<i-radius)head++;output[At(i)]=input[At(deque[head])];}}});}
 Pass(src,tmp,h,w,false);Pass(tmp,dst,w,h,true);return dst;}
 public static byte[] Holes(byte[] src,int w,int h){var seen=new System.Collections.BitArray(src.Length);var q=new Queue<int>();void Add(int i){if(!seen[i]&&src[i]<128){seen[i]=true;q.Enqueue(i);}}for(int x=0;x<w;x++){Add(x);Add((h-1)*w+x);}for(int y=0;y<h;y++){Add(y*w);Add(y*w+w-1);}while(q.Count>0){int i=q.Dequeue();if(i%w>0)Add(i-1);if(i%w<w-1)Add(i+1);if(i>=w)Add(i-w);if(i<src.Length-w)Add(i+w);}var r=(byte[])src.Clone();for(int i=0;i<r.Length;i++)if(!seen[i]&&r[i]<128)r[i]=255;return r;}
 public static void Brush(byte[] mask,int w,int h,double x,double y,double diameter,double soft,bool subtract){double r=diameter/2;for(int yy=Math.Max(0,(int)(y-r));yy<=Math.Min(h-1,(int)(y+r));yy++)for(int xx=Math.Max(0,(int)(x-r));xx<=Math.Min(w-1,(int)(x+r));xx++){double d=Math.Sqrt((xx-x)*(xx-x)+(yy-y)*(yy-y))/r;if(d>1)continue;byte a=(byte)(255*(soft==0?1:Math.Clamp((1-d)/soft,0,1)));int i=yy*w+xx;mask[i]=subtract?(byte)Math.Min(mask[i],255-a):Math.Max(mask[i],a);}}
 public static byte[] Polygon(int w,int h,List<(double x,double y)> points){var r=new byte[w*h];for(int y=0;y<h;y++){var cuts=new List<double>();for(int a=0,b=points.Count-1;a<points.Count;b=a++){var p=points[a];var q=points[b];if((p.y>y+.5)!=(q.y>y+.5))cuts.Add(p.x+(y+.5-p.y)*(q.x-p.x)/(q.y-p.y));}cuts.Sort();for(int a=0;a+1<cuts.Count;a+=2)for(int x=Math.Max(0,(int)Math.Ceiling(cuts[a]-.5));x<Math.Min(w,(int)Math.Ceiling(cuts[a+1]-.5));x++)r[y*w+x]=255;}return r;}
}
public class MotionSettings {
 public float speed=.1f,angle=90,tilingX=1,tilingY=1,rotation=0,emission=2,hdr=0;
 public string tint="#FFFFFF",shader="TextureFlow fallback";
}
public sealed class FlowSettings : MotionSettings {
 public int schemaVersion=1,activeLayer;
 public LayerConfig[] layers=[];
}
public sealed class LayerConfig {
 public string name="Area",maskFile="",flowFile="",flowName="";
 public bool enabled=true;
 public MotionSettings settings=new();
}
