Shader "TextureFlow/Masked Scroll" {
 Properties {
 _MainTex ("Base texture", 2D) = "white" {}
 _LayerCount ("Independent area count", Float) = 1
 _FlowTex ("Area 1 moving texture", 2D) = "white" {}
 _Mask ("Area 1 fixed mask", 2D) = "black" {}
 [HDR] _Tint ("Area 1 pattern tint", Color) = (1,1,1,1)
 _Emission ("Area 1 emission", Float) = 2
 _Velocity ("Area 1 velocity", Vector) = (0,0.1,0,0)
 _Rotation ("Area 1 rotation", Float) = 0
 _Enabled0 ("Area 1 enabled", Float) = 1
 _FlowTex1 ("Area 2 moving texture", 2D) = "white" {}
 _Mask1 ("Area 2 fixed mask", 2D) = "black" {}
 [HDR] _Tint1 ("Area 2 pattern tint", Color) = (1,1,1,1)
 _Emission1 ("Area 2 emission", Float) = 2
 _Velocity1 ("Area 2 velocity", Vector) = (0,0.1,0,0)
 _Rotation1 ("Area 2 rotation", Float) = 0
 _Enabled1 ("Area 2 enabled", Float) = 0
 _FlowTex2 ("Area 3 moving texture", 2D) = "white" {}
 _Mask2 ("Area 3 fixed mask", 2D) = "black" {}
 [HDR] _Tint2 ("Area 3 pattern tint", Color) = (1,1,1,1)
 _Emission2 ("Area 3 emission", Float) = 2
 _Velocity2 ("Area 3 velocity", Vector) = (0,0.1,0,0)
 _Rotation2 ("Area 3 rotation", Float) = 0
 _Enabled2 ("Area 3 enabled", Float) = 0
 _FlowTex3 ("Area 4 moving texture", 2D) = "white" {}
 _Mask3 ("Area 4 fixed mask", 2D) = "black" {}
 [HDR] _Tint3 ("Area 4 pattern tint", Color) = (1,1,1,1)
 _Emission3 ("Area 4 emission", Float) = 2
 _Velocity3 ("Area 4 velocity", Vector) = (0,0.1,0,0)
 _Rotation3 ("Area 4 rotation", Float) = 0
 _Enabled3 ("Area 4 enabled", Float) = 0
 _FlowTex4 ("Area 5 moving texture", 2D) = "white" {}
 _Mask4 ("Area 5 fixed mask", 2D) = "black" {}
 [HDR] _Tint4 ("Area 5 pattern tint", Color) = (1,1,1,1)
 _Emission4 ("Area 5 emission", Float) = 2
 _Velocity4 ("Area 5 velocity", Vector) = (0,0.1,0,0)
 _Rotation4 ("Area 5 rotation", Float) = 0
 _Enabled4 ("Area 5 enabled", Float) = 0
 _FlowTex5 ("Area 6 moving texture", 2D) = "white" {}
 _Mask5 ("Area 6 fixed mask", 2D) = "black" {}
 [HDR] _Tint5 ("Area 6 pattern tint", Color) = (1,1,1,1)
 _Emission5 ("Area 6 emission", Float) = 2
 _Velocity5 ("Area 6 velocity", Vector) = (0,0.1,0,0)
 _Rotation5 ("Area 6 rotation", Float) = 0
 _Enabled5 ("Area 6 enabled", Float) = 0
 _FlowTex6 ("Area 7 moving texture", 2D) = "white" {}
 _Mask6 ("Area 7 fixed mask", 2D) = "black" {}
 [HDR] _Tint6 ("Area 7 pattern tint", Color) = (1,1,1,1)
 _Emission6 ("Area 7 emission", Float) = 2
 _Velocity6 ("Area 7 velocity", Vector) = (0,0.1,0,0)
 _Rotation6 ("Area 7 rotation", Float) = 0
 _Enabled6 ("Area 7 enabled", Float) = 0
 _FlowTex7 ("Area 8 moving texture", 2D) = "white" {}
 _Mask7 ("Area 8 fixed mask", 2D) = "black" {}
 [HDR] _Tint7 ("Area 8 pattern tint", Color) = (1,1,1,1)
 _Emission7 ("Area 8 emission", Float) = 2
 _Velocity7 ("Area 8 velocity", Vector) = (0,0.1,0,0)
 _Rotation7 ("Area 8 rotation", Float) = 0
 _Enabled7 ("Area 8 enabled", Float) = 0
 }
 SubShader {
 Tags { "RenderType"="Opaque" "Queue"="Geometry" }
 LOD 200
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows
 #pragma target 4.0
 #include "UnityCG.cginc"
 sampler2D _MainTex;
 UNITY_DECLARE_TEX2D(_FlowTex);
 UNITY_DECLARE_TEX2D(_Mask);
 float _LayerCount;
 float4 _FlowTex_ST,_Tint,_Velocity;
 float _Emission,_Rotation,_Enabled0;
 UNITY_DECLARE_TEX2D_NOSAMPLER(_FlowTex1);
 UNITY_DECLARE_TEX2D_NOSAMPLER(_Mask1);
 float4 _FlowTex1_ST,_Tint1,_Velocity1;
 float _Emission1,_Rotation1,_Enabled1;
 UNITY_DECLARE_TEX2D_NOSAMPLER(_FlowTex2);
 UNITY_DECLARE_TEX2D_NOSAMPLER(_Mask2);
 float4 _FlowTex2_ST,_Tint2,_Velocity2;
 float _Emission2,_Rotation2,_Enabled2;
 UNITY_DECLARE_TEX2D_NOSAMPLER(_FlowTex3);
 UNITY_DECLARE_TEX2D_NOSAMPLER(_Mask3);
 float4 _FlowTex3_ST,_Tint3,_Velocity3;
 float _Emission3,_Rotation3,_Enabled3;
 UNITY_DECLARE_TEX2D_NOSAMPLER(_FlowTex4);
 UNITY_DECLARE_TEX2D_NOSAMPLER(_Mask4);
 float4 _FlowTex4_ST,_Tint4,_Velocity4;
 float _Emission4,_Rotation4,_Enabled4;
 UNITY_DECLARE_TEX2D_NOSAMPLER(_FlowTex5);
 UNITY_DECLARE_TEX2D_NOSAMPLER(_Mask5);
 float4 _FlowTex5_ST,_Tint5,_Velocity5;
 float _Emission5,_Rotation5,_Enabled5;
 UNITY_DECLARE_TEX2D_NOSAMPLER(_FlowTex6);
 UNITY_DECLARE_TEX2D_NOSAMPLER(_Mask6);
 float4 _FlowTex6_ST,_Tint6,_Velocity6;
 float _Emission6,_Rotation6,_Enabled6;
 UNITY_DECLARE_TEX2D_NOSAMPLER(_FlowTex7);
 UNITY_DECLARE_TEX2D_NOSAMPLER(_Mask7);
 float4 _FlowTex7_ST,_Tint7,_Velocity7;
 float _Emission7,_Rotation7,_Enabled7;
 struct Input { float2 uv_MainTex; };
 float2 FlowUV(float2 uv,float4 st,float4 velocity,float rotation) {
 float2 p=uv-.5;float s=sin(rotation),c=cos(rotation);
 return float2(c*p.x-s*p.y,s*p.x+c*p.y)*st.xy+.5+st.zw-velocity.xy*_Time.y;
 }
 void surf(Input IN,inout SurfaceOutputStandard o) {
 float2 uv=IN.uv_MainTex;fixed4 base=tex2D(_MainTex,uv);
 float3 albedo=base.rgb,emission=0;
 if(_LayerCount>0 && _Enabled0>0.5) {
 float alpha=saturate(UNITY_SAMPLE_TEX2D_SAMPLER(_Mask,_Mask,uv).r);
 float3 pattern=UNITY_SAMPLE_TEX2D_SAMPLER(_FlowTex,_FlowTex,FlowUV(uv,_FlowTex_ST,_Velocity,_Rotation)).rgb*_Tint.rgb;
 albedo=lerp(albedo,pattern,alpha);emission=lerp(emission,pattern*_Emission,alpha);
 }
 if(_LayerCount>1 && _Enabled1>0.5) {
 float alpha=saturate(UNITY_SAMPLE_TEX2D_SAMPLER(_Mask1,_Mask,uv).r);
 float3 pattern=UNITY_SAMPLE_TEX2D_SAMPLER(_FlowTex1,_FlowTex,FlowUV(uv,_FlowTex1_ST,_Velocity1,_Rotation1)).rgb*_Tint1.rgb;
 albedo=lerp(albedo,pattern,alpha);emission=lerp(emission,pattern*_Emission1,alpha);
 }
 if(_LayerCount>2 && _Enabled2>0.5) {
 float alpha=saturate(UNITY_SAMPLE_TEX2D_SAMPLER(_Mask2,_Mask,uv).r);
 float3 pattern=UNITY_SAMPLE_TEX2D_SAMPLER(_FlowTex2,_FlowTex,FlowUV(uv,_FlowTex2_ST,_Velocity2,_Rotation2)).rgb*_Tint2.rgb;
 albedo=lerp(albedo,pattern,alpha);emission=lerp(emission,pattern*_Emission2,alpha);
 }
 if(_LayerCount>3 && _Enabled3>0.5) {
 float alpha=saturate(UNITY_SAMPLE_TEX2D_SAMPLER(_Mask3,_Mask,uv).r);
 float3 pattern=UNITY_SAMPLE_TEX2D_SAMPLER(_FlowTex3,_FlowTex,FlowUV(uv,_FlowTex3_ST,_Velocity3,_Rotation3)).rgb*_Tint3.rgb;
 albedo=lerp(albedo,pattern,alpha);emission=lerp(emission,pattern*_Emission3,alpha);
 }
 if(_LayerCount>4 && _Enabled4>0.5) {
 float alpha=saturate(UNITY_SAMPLE_TEX2D_SAMPLER(_Mask4,_Mask,uv).r);
 float3 pattern=UNITY_SAMPLE_TEX2D_SAMPLER(_FlowTex4,_FlowTex,FlowUV(uv,_FlowTex4_ST,_Velocity4,_Rotation4)).rgb*_Tint4.rgb;
 albedo=lerp(albedo,pattern,alpha);emission=lerp(emission,pattern*_Emission4,alpha);
 }
 if(_LayerCount>5 && _Enabled5>0.5) {
 float alpha=saturate(UNITY_SAMPLE_TEX2D_SAMPLER(_Mask5,_Mask,uv).r);
 float3 pattern=UNITY_SAMPLE_TEX2D_SAMPLER(_FlowTex5,_FlowTex,FlowUV(uv,_FlowTex5_ST,_Velocity5,_Rotation5)).rgb*_Tint5.rgb;
 albedo=lerp(albedo,pattern,alpha);emission=lerp(emission,pattern*_Emission5,alpha);
 }
 if(_LayerCount>6 && _Enabled6>0.5) {
 float alpha=saturate(UNITY_SAMPLE_TEX2D_SAMPLER(_Mask6,_Mask,uv).r);
 float3 pattern=UNITY_SAMPLE_TEX2D_SAMPLER(_FlowTex6,_FlowTex,FlowUV(uv,_FlowTex6_ST,_Velocity6,_Rotation6)).rgb*_Tint6.rgb;
 albedo=lerp(albedo,pattern,alpha);emission=lerp(emission,pattern*_Emission6,alpha);
 }
 if(_LayerCount>7 && _Enabled7>0.5) {
 float alpha=saturate(UNITY_SAMPLE_TEX2D_SAMPLER(_Mask7,_Mask,uv).r);
 float3 pattern=UNITY_SAMPLE_TEX2D_SAMPLER(_FlowTex7,_FlowTex,FlowUV(uv,_FlowTex7_ST,_Velocity7,_Rotation7)).rgb*_Tint7.rgb;
 albedo=lerp(albedo,pattern,alpha);emission=lerp(emission,pattern*_Emission7,alpha);
 }
 o.Albedo=albedo;o.Emission=emission;o.Metallic=0;o.Smoothness=.2;o.Alpha=base.a;
 }
 ENDCG
 }
 Fallback "Diffuse"
}
