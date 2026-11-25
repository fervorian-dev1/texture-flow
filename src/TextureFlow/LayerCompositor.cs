using System.Windows.Media;
namespace TextureFlow;

public sealed record EffectFrame(Raster Flow,byte[] Mask,MotionSettings Settings);
public static class LayerCompositor
{
 public static Raster Render(Raster original,IReadOnlyList<EffectFrame> layers,double time,bool before=false,bool showBase=true,bool showGlow=true,byte[]? overlay=null,int maxWidth=640)
 {
  int width=Math.Min(maxWidth,original.W),height=Math.Max(1,(int)((double)width*original.H/original.W));var pixels=new byte[width*height*4];
  var frames=layers.Select(layer=>{
   if(layer.Mask.Length!=original.W*original.H)throw new ArgumentException("Area mask size does not match original texture.");
   var s=layer.Settings;var tint=(Color)ColorConverter.ConvertFromString(s.tint);double angle=s.angle*Math.PI/180,rotation=s.rotation*Math.PI/180;
   return new{layer.Flow,layer.Mask,s.tilingX,s.tilingY,cos=Math.Cos(rotation),sin=Math.Sin(rotation),dx=Math.Cos(angle)*s.speed*time,dy=Math.Sin(angle)*s.speed*time,gain=showGlow?1+s.emission*Math.Pow(2,s.hdr):1,tint};
  }).ToArray();
  Parallel.For(0,height,y=>{
   for(int x=0;x<width;x++){
    int source=(y*original.H/height*original.W+x*original.W/width),target=(y*width+x)*4;
    double b=showBase||before?original.Pixels[source*4]:32,g=showBase||before?original.Pixels[source*4+1]:32,r=showBase||before?original.Pixels[source*4+2]:32;
    if(!before)foreach(var frame in frames){
     double alpha=frame.Mask[source]/255.0;if(alpha==0)continue;
     double u=(x+.5)/width-.5,v=1-(y+.5)/height-.5;
     double fx=(u*frame.cos-v*frame.sin)*frame.tilingX+.5-frame.dx,fy=(u*frame.sin+v*frame.cos)*frame.tilingY+.5-frame.dy;
     int sx=Math.Min(frame.Flow.W-1,(int)((fx-Math.Floor(fx))*frame.Flow.W));int sy=Math.Min(frame.Flow.H-1,(int)((1-(fy-Math.Floor(fy)))*frame.Flow.H));int i=(sy*frame.Flow.W+sx)*4;
     b=b*(1-alpha)+frame.Flow.Pixels[i]*frame.tint.B/255.0*frame.gain*alpha;g=g*(1-alpha)+frame.Flow.Pixels[i+1]*frame.tint.G/255.0*frame.gain*alpha;r=r*(1-alpha)+frame.Flow.Pixels[i+2]*frame.tint.R/255.0*frame.gain*alpha;
    }
    if(!before&&overlay!=null){double a=overlay[source]/255.0*.25;b=b*(1-a)+180*a;g=g*(1-a)+80*a;r=r*(1-a)+255*a;}
    pixels[target]=(byte)Math.Clamp(b,0,255);pixels[target+1]=(byte)Math.Clamp(g,0,255);pixels[target+2]=(byte)Math.Clamp(r,0,255);pixels[target+3]=original.Pixels[source*4+3];
   }
  });
  return new(width,height,pixels);
 }
}
