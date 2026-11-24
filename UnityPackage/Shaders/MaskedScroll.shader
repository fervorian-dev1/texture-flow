Shader "TextureFlow/Masked Scroll" {
 Properties {
 _MainTex ("Base texture", 2D) = "white" {}
 _FlowTex ("Flow texture", 2D) = "white" {}
 _Mask ("Fixed selection mask", 2D) = "black" {}
 [HDR] _Tint ("Flow tint", Color) = (0.25,0.87,1,1)
 _Emission ("Emission", Float) = 2
 _Velocity ("Velocity UV per second", Vector) = (0,0.1,0,0)
 _Rotation ("Rotation radians", Float) = 0
 }
 SubShader {
 Tags { "RenderType"="Opaque" "Queue"="Geometry" }
 LOD 200
 CGPROGRAM
 #pragma surface surf Standard fullforwardshadows
 #pragma target 3.0
 sampler2D _MainTex,_FlowTex,_Mask;
 float4 _Tint,_Velocity,_FlowTex_ST;
 float _Emission,_Rotation;
 struct Input { float2 uv_MainTex; };
 void surf(Input IN,inout SurfaceOutputStandard o) {
 float2 uv=IN.uv_MainTex;
 float2 centered=uv-.5;
 float s=sin(_Rotation),c=cos(_Rotation);
 float2 fuv=float2(c*centered.x-s*centered.y,s*centered.x+c*centered.y)*_FlowTex_ST.xy+.5+_FlowTex_ST.zw-_Velocity.xy*_Time.y;
 fixed4 base=tex2D(_MainTex,uv);
 float mask=tex2D(_Mask,uv).r;
 float3 flow=tex2D(_FlowTex,fuv).rgb*_Tint.rgb;
 o.Albedo=lerp(base.rgb,flow,mask);
 o.Emission=flow*mask*_Emission;
 o.Metallic=0;o.Smoothness=.2;o.Alpha=base.a;
 }
 ENDCG
 }
 Fallback "Diffuse"
}
