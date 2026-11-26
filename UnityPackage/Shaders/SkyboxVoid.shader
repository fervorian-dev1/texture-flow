Shader "TextureFlow/Skybox Void" {
Properties {
 _MainTex ("Avatar picture",2D)="white" {}
 _AreaCount ("Openings",Float)=1
 _Sky0 ("Inside 1",2D)="black" {}
 _Mask0 ("Opening 1",2D)="black" {}
 _Look0 ("Style layers depth repeats 1",Vector)=(0,4,.6,1)
 _Motion0 ("Turn angle drift world 1",Vector)=(.05,0,.02,1)
 _Tint0 ("Tint brightness 1",Color)=(1,1,1,1)
 _Sky1 ("Inside 2",2D)="black" {}
 _Mask1 ("Opening 2",2D)="black" {}
 _Look1 ("Style layers depth repeats 2",Vector)=(0,4,.6,1)
 _Motion1 ("Turn angle drift world 2",Vector)=(.05,0,.02,1)
 _Tint1 ("Tint brightness 2",Color)=(1,1,1,1)
 _Sky2 ("Inside 3",2D)="black" {}
 _Mask2 ("Opening 3",2D)="black" {}
 _Look2 ("Style layers depth repeats 3",Vector)=(0,4,.6,1)
 _Motion2 ("Turn angle drift world 3",Vector)=(.05,0,.02,1)
 _Tint2 ("Tint brightness 3",Color)=(1,1,1,1)
 _Sky3 ("Inside 4",2D)="black" {}
 _Mask3 ("Opening 4",2D)="black" {}
 _Look3 ("Style layers depth repeats 4",Vector)=(0,4,.6,1)
 _Motion3 ("Turn angle drift world 4",Vector)=(.05,0,.02,1)
 _Tint3 ("Tint brightness 4",Color)=(1,1,1,1)
 _Sky4 ("Inside 5",2D)="black" {}
 _Mask4 ("Opening 5",2D)="black" {}
 _Look4 ("Style layers depth repeats 5",Vector)=(0,4,.6,1)
 _Motion4 ("Turn angle drift world 5",Vector)=(.05,0,.02,1)
 _Tint4 ("Tint brightness 5",Color)=(1,1,1,1)
 _Sky5 ("Inside 6",2D)="black" {}
 _Mask5 ("Opening 6",2D)="black" {}
 _Look5 ("Style layers depth repeats 6",Vector)=(0,4,.6,1)
 _Motion5 ("Turn angle drift world 6",Vector)=(.05,0,.02,1)
 _Tint5 ("Tint brightness 6",Color)=(1,1,1,1)
 _Sky6 ("Inside 7",2D)="black" {}
 _Mask6 ("Opening 7",2D)="black" {}
 _Look6 ("Style layers depth repeats 7",Vector)=(0,4,.6,1)
 _Motion6 ("Turn angle drift world 7",Vector)=(.05,0,.02,1)
 _Tint6 ("Tint brightness 7",Color)=(1,1,1,1)
 _Sky7 ("Inside 8",2D)="black" {}
 _Mask7 ("Opening 8",2D)="black" {}
 _Look7 ("Style layers depth repeats 8",Vector)=(0,4,.6,1)
 _Motion7 ("Turn angle drift world 8",Vector)=(.05,0,.02,1)
 _Tint7 ("Tint brightness 8",Color)=(1,1,1,1)
}
SubShader {Tags {"RenderType"="Opaque" "Queue"="Geometry"} Pass {CGPROGRAM
#pragma vertex vert
#pragma fragment frag
#pragma target 4.0
#pragma multi_compile_instancing
#include "UnityCG.cginc"
 sampler2D _MainTex; float _AreaCount;
 UNITY_DECLARE_TEX2D(_Sky0); UNITY_DECLARE_TEX2D(_Mask0); float4 _Sky0_TexelSize,_Look0,_Motion0,_Tint0;
 UNITY_DECLARE_TEX2D_NOSAMPLER(_Sky1); UNITY_DECLARE_TEX2D_NOSAMPLER(_Mask1); float4 _Sky1_TexelSize,_Look1,_Motion1,_Tint1;
 UNITY_DECLARE_TEX2D_NOSAMPLER(_Sky2); UNITY_DECLARE_TEX2D_NOSAMPLER(_Mask2); float4 _Sky2_TexelSize,_Look2,_Motion2,_Tint2;
 UNITY_DECLARE_TEX2D_NOSAMPLER(_Sky3); UNITY_DECLARE_TEX2D_NOSAMPLER(_Mask3); float4 _Sky3_TexelSize,_Look3,_Motion3,_Tint3;
 UNITY_DECLARE_TEX2D_NOSAMPLER(_Sky4); UNITY_DECLARE_TEX2D_NOSAMPLER(_Mask4); float4 _Sky4_TexelSize,_Look4,_Motion4,_Tint4;
 UNITY_DECLARE_TEX2D_NOSAMPLER(_Sky5); UNITY_DECLARE_TEX2D_NOSAMPLER(_Mask5); float4 _Sky5_TexelSize,_Look5,_Motion5,_Tint5;
 UNITY_DECLARE_TEX2D_NOSAMPLER(_Sky6); UNITY_DECLARE_TEX2D_NOSAMPLER(_Mask6); float4 _Sky6_TexelSize,_Look6,_Motion6,_Tint6;
 UNITY_DECLARE_TEX2D_NOSAMPLER(_Sky7); UNITY_DECLARE_TEX2D_NOSAMPLER(_Mask7); float4 _Sky7_TexelSize,_Look7,_Motion7,_Tint7;

struct appdata {float4 vertex:POSITION;float2 uv:TEXCOORD0;float3 normal:NORMAL;float4 tangent:TANGENT;UNITY_VERTEX_INPUT_INSTANCE_ID};
struct v2f {float4 pos:SV_POSITION;float2 uv:TEXCOORD0;float3 world:TEXCOORD1;float3 normal:TEXCOORD2;float3 tangent:TEXCOORD3;float3 bitangent:TEXCOORD4;UNITY_VERTEX_OUTPUT_STEREO};
v2f vert(appdata v){v2f o;UNITY_SETUP_INSTANCE_ID(v);UNITY_INITIALIZE_OUTPUT(v2f,o);UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);o.pos=UnityObjectToClipPos(v.vertex);o.uv=v.uv;o.world=mul(unity_ObjectToWorld,v.vertex).xyz;o.normal=UnityObjectToWorldNormal(v.normal);o.tangent=UnityObjectToWorldDir(v.tangent.xyz);o.bitangent=cross(o.normal,o.tangent)*v.tangent.w*unity_WorldTransformParams.w;return o;}
float2 rotate2(float2 p,float a){float s,c;sincos(a,s,c);return float2(c*p.x-s*p.y,s*p.x+c*p.y);}
float3 area0(v2f v,float3 ray){float4 look=_Look0,motion=_Motion0;float turn=motion.y+_Time.y*motion.x;float3 result=0;
 if(look.x<.5){float3 dir=ray;if(motion.w<.5)dir=normalize(mul((float3x3)unity_WorldToObject,ray));float2 uv=float2(atan2(dir.x,dir.z)/6.2831853+.5+turn/6.2831853,asin(clamp(dir.y,-1,1))/3.14159265+.5);uv.y=clamp(uv.y,_Sky0_TexelSize.y*.5,1-_Sky0_TexelSize.y*.5);result=UNITY_SAMPLE_TEX2D_SAMPLER(_Sky0,_Sky0,uv).rgb;}
 else{float3 tangentRay=float3(dot(ray,normalize(v.tangent)),dot(ray,normalize(v.bitangent)),dot(ray,normalize(v.normal)));float2 offset=tangentRay.xy/max(abs(tangentRay.z),.12);float occupied=0;[loop]for(int k=0;k<8;k++){if(k>=look.y)break;float depth=look.z*(k+1)/look.y;float2 p=(v.uv-.5)*look.w+offset*depth;if(look.x>1.5)p*=1+depth*1.2;float2 uv=rotate2(p,turn+(look.x>1.5?k*.35:0))+.5;uv.y-=_Time.y*motion.z;float3 sample=UNITY_SAMPLE_TEX2D_SAMPLER(_Sky0,_Sky0,uv).rgb;float opacity=saturate((max(sample.r,max(sample.g,sample.b))-.08)*1.5);result+=(1-occupied)*sample*opacity*pow(.82,k);occupied+=(1-occupied)*opacity;}}return result*_Tint0.rgb;}
float3 area1(v2f v,float3 ray){float4 look=_Look1,motion=_Motion1;float turn=motion.y+_Time.y*motion.x;float3 result=0;
 if(look.x<.5){float3 dir=ray;if(motion.w<.5)dir=normalize(mul((float3x3)unity_WorldToObject,ray));float2 uv=float2(atan2(dir.x,dir.z)/6.2831853+.5+turn/6.2831853,asin(clamp(dir.y,-1,1))/3.14159265+.5);uv.y=clamp(uv.y,_Sky1_TexelSize.y*.5,1-_Sky1_TexelSize.y*.5);result=UNITY_SAMPLE_TEX2D_SAMPLER(_Sky1,_Sky0,uv).rgb;}
 else{float3 tangentRay=float3(dot(ray,normalize(v.tangent)),dot(ray,normalize(v.bitangent)),dot(ray,normalize(v.normal)));float2 offset=tangentRay.xy/max(abs(tangentRay.z),.12);float occupied=0;[loop]for(int k=0;k<8;k++){if(k>=look.y)break;float depth=look.z*(k+1)/look.y;float2 p=(v.uv-.5)*look.w+offset*depth;if(look.x>1.5)p*=1+depth*1.2;float2 uv=rotate2(p,turn+(look.x>1.5?k*.35:0))+.5;uv.y-=_Time.y*motion.z;float3 sample=UNITY_SAMPLE_TEX2D_SAMPLER(_Sky1,_Sky0,uv).rgb;float opacity=saturate((max(sample.r,max(sample.g,sample.b))-.08)*1.5);result+=(1-occupied)*sample*opacity*pow(.82,k);occupied+=(1-occupied)*opacity;}}return result*_Tint1.rgb;}
float3 area2(v2f v,float3 ray){float4 look=_Look2,motion=_Motion2;float turn=motion.y+_Time.y*motion.x;float3 result=0;
 if(look.x<.5){float3 dir=ray;if(motion.w<.5)dir=normalize(mul((float3x3)unity_WorldToObject,ray));float2 uv=float2(atan2(dir.x,dir.z)/6.2831853+.5+turn/6.2831853,asin(clamp(dir.y,-1,1))/3.14159265+.5);uv.y=clamp(uv.y,_Sky2_TexelSize.y*.5,1-_Sky2_TexelSize.y*.5);result=UNITY_SAMPLE_TEX2D_SAMPLER(_Sky2,_Sky0,uv).rgb;}
 else{float3 tangentRay=float3(dot(ray,normalize(v.tangent)),dot(ray,normalize(v.bitangent)),dot(ray,normalize(v.normal)));float2 offset=tangentRay.xy/max(abs(tangentRay.z),.12);float occupied=0;[loop]for(int k=0;k<8;k++){if(k>=look.y)break;float depth=look.z*(k+1)/look.y;float2 p=(v.uv-.5)*look.w+offset*depth;if(look.x>1.5)p*=1+depth*1.2;float2 uv=rotate2(p,turn+(look.x>1.5?k*.35:0))+.5;uv.y-=_Time.y*motion.z;float3 sample=UNITY_SAMPLE_TEX2D_SAMPLER(_Sky2,_Sky0,uv).rgb;float opacity=saturate((max(sample.r,max(sample.g,sample.b))-.08)*1.5);result+=(1-occupied)*sample*opacity*pow(.82,k);occupied+=(1-occupied)*opacity;}}return result*_Tint2.rgb;}
float3 area3(v2f v,float3 ray){float4 look=_Look3,motion=_Motion3;float turn=motion.y+_Time.y*motion.x;float3 result=0;
 if(look.x<.5){float3 dir=ray;if(motion.w<.5)dir=normalize(mul((float3x3)unity_WorldToObject,ray));float2 uv=float2(atan2(dir.x,dir.z)/6.2831853+.5+turn/6.2831853,asin(clamp(dir.y,-1,1))/3.14159265+.5);uv.y=clamp(uv.y,_Sky3_TexelSize.y*.5,1-_Sky3_TexelSize.y*.5);result=UNITY_SAMPLE_TEX2D_SAMPLER(_Sky3,_Sky0,uv).rgb;}
 else{float3 tangentRay=float3(dot(ray,normalize(v.tangent)),dot(ray,normalize(v.bitangent)),dot(ray,normalize(v.normal)));float2 offset=tangentRay.xy/max(abs(tangentRay.z),.12);float occupied=0;[loop]for(int k=0;k<8;k++){if(k>=look.y)break;float depth=look.z*(k+1)/look.y;float2 p=(v.uv-.5)*look.w+offset*depth;if(look.x>1.5)p*=1+depth*1.2;float2 uv=rotate2(p,turn+(look.x>1.5?k*.35:0))+.5;uv.y-=_Time.y*motion.z;float3 sample=UNITY_SAMPLE_TEX2D_SAMPLER(_Sky3,_Sky0,uv).rgb;float opacity=saturate((max(sample.r,max(sample.g,sample.b))-.08)*1.5);result+=(1-occupied)*sample*opacity*pow(.82,k);occupied+=(1-occupied)*opacity;}}return result*_Tint3.rgb;}
float3 area4(v2f v,float3 ray){float4 look=_Look4,motion=_Motion4;float turn=motion.y+_Time.y*motion.x;float3 result=0;
 if(look.x<.5){float3 dir=ray;if(motion.w<.5)dir=normalize(mul((float3x3)unity_WorldToObject,ray));float2 uv=float2(atan2(dir.x,dir.z)/6.2831853+.5+turn/6.2831853,asin(clamp(dir.y,-1,1))/3.14159265+.5);uv.y=clamp(uv.y,_Sky4_TexelSize.y*.5,1-_Sky4_TexelSize.y*.5);result=UNITY_SAMPLE_TEX2D_SAMPLER(_Sky4,_Sky0,uv).rgb;}
 else{float3 tangentRay=float3(dot(ray,normalize(v.tangent)),dot(ray,normalize(v.bitangent)),dot(ray,normalize(v.normal)));float2 offset=tangentRay.xy/max(abs(tangentRay.z),.12);float occupied=0;[loop]for(int k=0;k<8;k++){if(k>=look.y)break;float depth=look.z*(k+1)/look.y;float2 p=(v.uv-.5)*look.w+offset*depth;if(look.x>1.5)p*=1+depth*1.2;float2 uv=rotate2(p,turn+(look.x>1.5?k*.35:0))+.5;uv.y-=_Time.y*motion.z;float3 sample=UNITY_SAMPLE_TEX2D_SAMPLER(_Sky4,_Sky0,uv).rgb;float opacity=saturate((max(sample.r,max(sample.g,sample.b))-.08)*1.5);result+=(1-occupied)*sample*opacity*pow(.82,k);occupied+=(1-occupied)*opacity;}}return result*_Tint4.rgb;}
float3 area5(v2f v,float3 ray){float4 look=_Look5,motion=_Motion5;float turn=motion.y+_Time.y*motion.x;float3 result=0;
 if(look.x<.5){float3 dir=ray;if(motion.w<.5)dir=normalize(mul((float3x3)unity_WorldToObject,ray));float2 uv=float2(atan2(dir.x,dir.z)/6.2831853+.5+turn/6.2831853,asin(clamp(dir.y,-1,1))/3.14159265+.5);uv.y=clamp(uv.y,_Sky5_TexelSize.y*.5,1-_Sky5_TexelSize.y*.5);result=UNITY_SAMPLE_TEX2D_SAMPLER(_Sky5,_Sky0,uv).rgb;}
 else{float3 tangentRay=float3(dot(ray,normalize(v.tangent)),dot(ray,normalize(v.bitangent)),dot(ray,normalize(v.normal)));float2 offset=tangentRay.xy/max(abs(tangentRay.z),.12);float occupied=0;[loop]for(int k=0;k<8;k++){if(k>=look.y)break;float depth=look.z*(k+1)/look.y;float2 p=(v.uv-.5)*look.w+offset*depth;if(look.x>1.5)p*=1+depth*1.2;float2 uv=rotate2(p,turn+(look.x>1.5?k*.35:0))+.5;uv.y-=_Time.y*motion.z;float3 sample=UNITY_SAMPLE_TEX2D_SAMPLER(_Sky5,_Sky0,uv).rgb;float opacity=saturate((max(sample.r,max(sample.g,sample.b))-.08)*1.5);result+=(1-occupied)*sample*opacity*pow(.82,k);occupied+=(1-occupied)*opacity;}}return result*_Tint5.rgb;}
float3 area6(v2f v,float3 ray){float4 look=_Look6,motion=_Motion6;float turn=motion.y+_Time.y*motion.x;float3 result=0;
 if(look.x<.5){float3 dir=ray;if(motion.w<.5)dir=normalize(mul((float3x3)unity_WorldToObject,ray));float2 uv=float2(atan2(dir.x,dir.z)/6.2831853+.5+turn/6.2831853,asin(clamp(dir.y,-1,1))/3.14159265+.5);uv.y=clamp(uv.y,_Sky6_TexelSize.y*.5,1-_Sky6_TexelSize.y*.5);result=UNITY_SAMPLE_TEX2D_SAMPLER(_Sky6,_Sky0,uv).rgb;}
 else{float3 tangentRay=float3(dot(ray,normalize(v.tangent)),dot(ray,normalize(v.bitangent)),dot(ray,normalize(v.normal)));float2 offset=tangentRay.xy/max(abs(tangentRay.z),.12);float occupied=0;[loop]for(int k=0;k<8;k++){if(k>=look.y)break;float depth=look.z*(k+1)/look.y;float2 p=(v.uv-.5)*look.w+offset*depth;if(look.x>1.5)p*=1+depth*1.2;float2 uv=rotate2(p,turn+(look.x>1.5?k*.35:0))+.5;uv.y-=_Time.y*motion.z;float3 sample=UNITY_SAMPLE_TEX2D_SAMPLER(_Sky6,_Sky0,uv).rgb;float opacity=saturate((max(sample.r,max(sample.g,sample.b))-.08)*1.5);result+=(1-occupied)*sample*opacity*pow(.82,k);occupied+=(1-occupied)*opacity;}}return result*_Tint6.rgb;}
float3 area7(v2f v,float3 ray){float4 look=_Look7,motion=_Motion7;float turn=motion.y+_Time.y*motion.x;float3 result=0;
 if(look.x<.5){float3 dir=ray;if(motion.w<.5)dir=normalize(mul((float3x3)unity_WorldToObject,ray));float2 uv=float2(atan2(dir.x,dir.z)/6.2831853+.5+turn/6.2831853,asin(clamp(dir.y,-1,1))/3.14159265+.5);uv.y=clamp(uv.y,_Sky7_TexelSize.y*.5,1-_Sky7_TexelSize.y*.5);result=UNITY_SAMPLE_TEX2D_SAMPLER(_Sky7,_Sky0,uv).rgb;}
 else{float3 tangentRay=float3(dot(ray,normalize(v.tangent)),dot(ray,normalize(v.bitangent)),dot(ray,normalize(v.normal)));float2 offset=tangentRay.xy/max(abs(tangentRay.z),.12);float occupied=0;[loop]for(int k=0;k<8;k++){if(k>=look.y)break;float depth=look.z*(k+1)/look.y;float2 p=(v.uv-.5)*look.w+offset*depth;if(look.x>1.5)p*=1+depth*1.2;float2 uv=rotate2(p,turn+(look.x>1.5?k*.35:0))+.5;uv.y-=_Time.y*motion.z;float3 sample=UNITY_SAMPLE_TEX2D_SAMPLER(_Sky7,_Sky0,uv).rgb;float opacity=saturate((max(sample.r,max(sample.g,sample.b))-.08)*1.5);result+=(1-occupied)*sample*opacity*pow(.82,k);occupied+=(1-occupied)*opacity;}}return result*_Tint7.rgb;}
float4 frag(v2f i):SV_Target {UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);float3 ray=normalize(i.world-_WorldSpaceCameraPos);float3 color=tex2D(_MainTex,i.uv).rgb;
 if(_AreaCount>0.5)color=lerp(color,area0(i,ray),UNITY_SAMPLE_TEX2D_SAMPLER(_Mask0,_Mask0,i.uv).r);
 if(_AreaCount>1.5)color=lerp(color,area1(i,ray),UNITY_SAMPLE_TEX2D_SAMPLER(_Mask1,_Mask0,i.uv).r);
 if(_AreaCount>2.5)color=lerp(color,area2(i,ray),UNITY_SAMPLE_TEX2D_SAMPLER(_Mask2,_Mask0,i.uv).r);
 if(_AreaCount>3.5)color=lerp(color,area3(i,ray),UNITY_SAMPLE_TEX2D_SAMPLER(_Mask3,_Mask0,i.uv).r);
 if(_AreaCount>4.5)color=lerp(color,area4(i,ray),UNITY_SAMPLE_TEX2D_SAMPLER(_Mask4,_Mask0,i.uv).r);
 if(_AreaCount>5.5)color=lerp(color,area5(i,ray),UNITY_SAMPLE_TEX2D_SAMPLER(_Mask5,_Mask0,i.uv).r);
 if(_AreaCount>6.5)color=lerp(color,area6(i,ray),UNITY_SAMPLE_TEX2D_SAMPLER(_Mask6,_Mask0,i.uv).r);
 if(_AreaCount>7.5)color=lerp(color,area7(i,ray),UNITY_SAMPLE_TEX2D_SAMPLER(_Mask7,_Mask0,i.uv).r);
return float4(color,1);}
ENDCG
}}
Fallback Off
}
