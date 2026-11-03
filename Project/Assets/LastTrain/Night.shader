Shader "TrainLevel/Night"
{
 Properties { [PerRendererData] _MainTex("Image",2D)="white"{} _Color("Lighting",Color)=(1,1,1,1) _Mode("Surface",Float)=0 _Spill("Reflected light",Color)=(0,0,0,0) }
 SubShader {
 Tags {"Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True"}
 Cull Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha
 Pass {
 CGPROGRAM
 #pragma vertex vert
 #pragma fragment frag
 #include "UnityCG.cginc"
 sampler2D _MainTex; float4 _MainTex_ST,_MainTex_TexelSize; fixed4 _Color,_Spill; float _Mode;
 int _NeonCount;float4 _NeonPositions[8],_NeonColors[8];
 int _StationCount;float4 _StationPositions[8],_StationColors[8];
 float _IntroLighting,_IntroBlur;
 struct V {float4 p:POSITION;float2 uv:TEXCOORD0;fixed4 c:COLOR;};
 struct F {float4 p:SV_POSITION;float2 uv:TEXCOORD0;float2 world:TEXCOORD1;fixed4 c:COLOR;};
 F vert(V v){F o;o.p=UnityObjectToClipPos(v.p);o.uv=TRANSFORM_TEX(v.uv,_MainTex);o.world=mul(unity_ObjectToWorld,v.p).xy;o.c=v.c;return o;}
 fixed4 frag(F i):SV_Target {
  if(_Mode>2.5){
   float2 pixel=_MainTex_TexelSize.xy*4;float2 uv=(floor(i.uv/pixel)+.5)*pixel;
   fixed4 signColor=tex2D(_MainTex,uv);float3 neon=signColor.rgb;
   #ifndef UNITY_COLORSPACE_GAMMA
   neon=LinearToGammaSpace(neon);
   #endif
   float brightness=max(neon.r,max(neon.g,neon.b));
   float pink=saturate((neon.r-neon.g)*6)*saturate((neon.b-neon.g+.12)*8);
   float highlight=smoothstep(.18,.82,neon.g/max(brightness,.001));
   neon=lerp(neon,brightness*lerp(float3(1,.05,.86),float3(1,.72,.96),highlight),pink);
   #ifndef UNITY_COLORSPACE_GAMMA
   neon=GammaToLinearSpace(neon);
   #endif
   return fixed4(neon,signColor.a)*i.c*_Color;
  }
  fixed4 s=tex2D(_MainTex,i.uv);
  if(_Mode<.5 && _IntroBlur>0){float2 d=float2(_IntroBlur,0);s=(s*2+tex2D(_MainTex,i.uv-d)+tex2D(_MainTex,i.uv+d)+tex2D(_MainTex,i.uv-d*2)+tex2D(_MainTex,i.uv+d*2))/6;}
  float3 c=s.rgb;
  #ifndef UNITY_COLORSPACE_GAMMA
  c=LinearToGammaSpace(c);
  #endif
  float l=dot(c,float3(.2126,.7152,.0722));float value=max(c.r,max(c.g,c.b));float3 result;
  if(_Mode<.5){
   float blue=saturate((c.b-c.r)*4)*saturate((c.b-c.g*.8)*5);
   float warm=saturate((c.r-c.b)*3);
   result=lerp(float3(.035,.045,.065)+l*float3(.22,.24,.29),float3(.035,.065,.09)+l*float3(.24,.27,.32),blue);
   result=lerp(result,float3(.065,.03,.085)+l*float3(.36,.17,.34),warm*.65);
   float fixture=smoothstep(.83,.99,l)*(1-blue);
   result=lerp(result,float3(1,.58,.94),fixture*.85);
   float2 samplePosition=floor(i.uv*_MainTex_TexelSize.zw)/_MainTex_TexelSize.zw;
   for(int n=0;n<8;n++)if(n<_NeonCount){float2 d=(samplePosition-_NeonPositions[n].xy)/_NeonPositions[n].zw;float light=pow(saturate(1-length(d)),2);result+=_NeonColors[n].rgb*light*(.26+l*.65);}
   result=lerp(result,float3(.025,.035,.055)+l*float3(.26,.28,.32),_IntroLighting);
  }else if(_Mode<1.5){
   result=value<.17?float3(.035,.045,.065):value<.46?float3(.10,.12,.16):value<.80?float3(.19,.21,.26):float3(.34,.37,.43);
   result+=(l-value*.5)*.035;
   float roof=step(.15,i.uv.x)*step(i.uv.x,.85)*step(123.5,floor(i.uv.y*128))*step(floor(i.uv.y*128),124.5);
   float cyan=step(.78,frac(floor(i.uv.x*632/24)*.618));
   result=lerp(result,lerp(float3(1,.58,.94),float3(.55,.96,1),cyan),roof*(1-_IntroLighting));
   float sign=step(.85,value)*step(.42,i.uv.y)*step(i.uv.y,.48)*step(.46,i.uv.x)*step(i.uv.x,.55);
   result=lerp(result,float3(.5,.9,1),sign*(1-_IntroLighting));
   float3 spill=0;float2 surface=floor(i.world/.049)*.049;
   for(int n=0;n<8;n++)if(n<_StationCount){float2 d=(surface-_StationPositions[n].xy)*float2(1,.6);float light=saturate(1-length(d)/_StationPositions[n].z);spill+=_StationColors[n].rgb*light*light;}
   result+=spill*(.24+value*.42);
  }else result=value<.17?float3(.025,.035,.05):float3(.4,.4,.4)+value*.6;
  #ifndef UNITY_COLORSPACE_GAMMA
  result=GammaToLinearSpace(result);
  #endif
  return fixed4(result*i.c.rgb*_Color.rgb,s.a*i.c.a*_Color.a);
 }
 ENDCG
 }
 }
}



