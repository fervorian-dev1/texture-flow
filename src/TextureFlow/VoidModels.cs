using System.Numerics;
using System.Windows.Media;
namespace TextureFlow;

public sealed class VoidLookSettings
{
 public int style=0,steps=4;
 public float yawSpeed=3,depth=.6f,tiling=1,brightness=1,drift=.02f,angle=0;
 public bool worldLocked=true;
 public string tint="#FFFFFF";
 public VoidLookSettings Copy()=>new(){style=style,steps=steps,yawSpeed=yawSpeed,depth=depth,tiling=tiling,brightness=brightness,drift=drift,angle=angle,worldLocked=worldLocked,tint=tint};
 public static VoidLookSettings Preset(int style)=>style switch{1=>new(){style=1,depth=.8f,steps=4,tiling=1.5f,yawSpeed=2,drift=.015f},2=>new(){style=2,depth=1.2f,steps=6,tiling=1.5f,yawSpeed=10,drift=.025f},_=>new()};
 public void Validate(){bool R(float f,float a,float b)=>float.IsFinite(f)&&f>=a&&f<=b;if(style<0||style>2||steps<1||steps>8||!R(yawSpeed,-45,45)||!R(depth,.05f,2)||!R(tiling,.1f,8)||!R(brightness,0,8)||!R(drift,-.2f,.2f)||!R(angle,0,360))throw new InvalidOperationException("Choose a look and keep its sliders inside their ranges.");if(tint==null||tint.Length!=7||tint[0]!='#'||!tint.Skip(1).All(Uri.IsHexDigit))throw new InvalidOperationException("Color needs # followed by six letters/numbers, such as #FFFFFF for white.");}
}
public sealed record VoidFrame(Raster Picture,byte[] Mask,VoidLookSettings Look);
public static class VoidPreview
{
 public static Vector2 SkyUv(Vector3 ray,double yaw){ray=Vector3.Normalize(ray);return new((float)(Math.Atan2(ray.X,ray.Z)/(2*Math.PI)+.5+yaw/(2*Math.PI)),(float)(Math.Asin(Math.Clamp(ray.Y,-1,1))/Math.PI+.5));}
 static Vector3 Sample(Raster im,double u,double v,bool clampV){u-=Math.Floor(u);v=clampV?Math.Clamp(v,.5/im.H,1-.5/im.H):v-Math.Floor(v);int x=Math.Clamp((int)(u*im.W),0,im.W-1),y=Math.Clamp((int)((1-v)*im.H),0,im.H-1),i=(y*im.W+x)*4;return new(im.Pixels[i+2]/255f,im.Pixels[i+1]/255f,im.Pixels[i]/255f);}
 public static Raster Render(Raster original,IReadOnlyList<VoidFrame> frames,double time,float lookYaw=0,float lookPitch=0,int width=560)
 {
  width=Math.Min(width,original.W);int height=Math.Max(1,(int)((double)width*original.H/original.W));var pixels=new byte[width*height*4];double cy=lookYaw*Math.PI/180,cp=lookPitch*Math.PI/180;var eye=new Vector3((float)(Math.Sin(cy)*Math.Cos(cp)*2),(float)(Math.Sin(cp)*2),(float)(Math.Cos(cy)*Math.Cos(cp)*2));
  var data=frames.Select(f=>{f.Look.Validate();if(f.Mask.Length!=original.W*original.H)throw new ArgumentException("The opening mask does not match your avatar picture.");var c=(Color)ColorConverter.ConvertFromString(f.Look.tint);return new{f.Picture,f.Mask,f.Look,tint=new Vector3(c.R/255f,c.G/255f,c.B/255f)};}).ToArray();
  Parallel.For(0,height,y=>{for(int x=0;x<width;x++){
   float u=(x+.5f)/width,v=1-(y+.5f)/height;int index=(y*original.H/height*original.W+x*original.W/width),o=(y*width+x)*4;Vector3 color=new(original.Pixels[index*4+2]/255f,original.Pixels[index*4+1]/255f,original.Pixels[index*4]/255f);var ray=Vector3.Normalize(new Vector3((u-.5f)*2,(v-.5f)*2,0)-eye);var offset=new Vector2(ray.X,ray.Y)/Math.Max(Math.Abs(ray.Z),.12f);
   foreach(var f in data){float alpha=f.Mask[index]/255f;if(alpha==0)continue;var s=f.Look;double rotation=(s.angle+s.yawSpeed*time)*Math.PI/180;Vector3 inside;
    if(s.style==0){var uv=SkyUv(ray,rotation);inside=Sample(f.Picture,uv.X,uv.Y,true);}
    else {inside=Vector3.Zero;float occupied=0;for(int layer=0;layer<s.steps;layer++){float depth=s.depth*(layer+1)/s.steps;var p=new Vector2(u-.5f,v-.5f)*s.tiling+offset*depth;double a=rotation+(s.style==2?layer*.35:0);if(s.style==2)p*=1+depth*1.2f;var uv=new Vector2((float)(Math.Cos(a)*p.X-Math.Sin(a)*p.Y)+.5f,(float)(Math.Sin(a)*p.X+Math.Cos(a)*p.Y)+.5f-(float)(time*s.drift));var sample=Sample(f.Picture,uv.X,uv.Y,false);float light=Math.Max(sample.X,Math.Max(sample.Y,sample.Z));float opacity=Math.Clamp((light-.08f)*1.5f,0,1);float fade=(float)Math.Pow(.82,layer);inside+=(1-occupied)*sample*opacity*fade;occupied+=(1-occupied)*opacity;}}
    color=Vector3.Lerp(color,inside*f.tint*s.brightness,alpha);
   }
   pixels[o]=(byte)Math.Clamp(color.Z*255,0,255);pixels[o+1]=(byte)Math.Clamp(color.Y*255,0,255);pixels[o+2]=(byte)Math.Clamp(color.X*255,0,255);pixels[o+3]=255;
  }});return new(width,height,pixels);
 }
 public static Raster Stars(int width=1024,int height=512)
 {
  var p=new byte[width*height*4];for(int y=0;y<height;y++)for(int x=0;x<width;x++){int i=(y*width+x)*4;double mist=Math.Pow(Math.Max(0,Math.Sin(x*4*Math.PI/width+Math.Sin(y*.023))*Math.Sin(y*.009+x*2*Math.PI/width)),3);p[i]=(byte)(4+mist*22);p[i+1]=(byte)(2+mist*9);p[i+2]=(byte)(3+mist*12);p[i+3]=255;}
  var random=new Random(314159);for(int n=0;n<1600;n++){int x=random.Next(width),y=(int)((Math.Asin(random.NextDouble()*2-1)/Math.PI+.5)*(height-1)),size=n%29==0?3:1;for(int yy=Math.Max(0,y-size);yy<=Math.Min(height-1,y+size);yy++)for(int xx=x-size;xx<=x+size;xx++){int i=(yy*width+(xx+width)%width)*4;double a=1.0/(1+Math.Abs(xx-x)+Math.Abs(yy-y));byte bright=(byte)(140+random.Next(116));p[i]=(byte)Math.Max(p[i],bright*a);p[i+1]=(byte)Math.Max(p[i+1],bright*.95*a);p[i+2]=(byte)Math.Max(p[i+2],bright*.9*a);}}
  return new(width,height,p);
 }
}
