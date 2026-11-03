Shader "TrainLevel/Equipment"
{
    Properties
    {
        [PerRendererData] _MainTex("Sprite", 2D) = "white" {}
        _Color("Lighting", Color) = (1,1,1,1)
        _Spill("Reflected light", Color) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "CanUseSpriteAtlas"="True" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _Spill;
            int _StationCount;float4 _StationPositions[8],_StationColors[8];
            struct Vertex { float4 position:POSITION; float2 uv:TEXCOORD0; fixed4 color:COLOR; };
            struct Pixel { float4 position:SV_POSITION; float2 uv:TEXCOORD0; float2 world:TEXCOORD1; fixed4 color:COLOR; };
            Pixel vert(Vertex v) { Pixel o; o.position=UnityObjectToClipPos(v.position);o.uv=v.uv;o.world=mul(unity_ObjectToWorld,v.position).xy;o.color=v.color;return o; }
            fixed4 frag(Pixel p):SV_Target
            {
                fixed4 source=tex2D(_MainTex,p.uv);
                float3 rgb=source.rgb;
                #ifndef UNITY_COLORSPACE_GAMMA
                rgb=LinearToGammaSpace(rgb);
                #endif
                float value=max(rgb.r,max(rgb.g,rgb.b));
                float3 palette=value<.12?float3(16,24,35)/255.0:
                    value<.37?float3(40,56,72)/255.0:
                    value<.66?float3(78,96,112)/255.0:float3(163,223,229)/255.0;
                if(rgb.r>rgb.b*1.3 && rgb.r>rgb.g*1.1 && value>.35)palette=float3(230,175,78)/255.0;
                float3 spill=0;float2 surface=floor(p.world/.049)*.049;
                for(int n=0;n<8;n++)if(n<_StationCount){float2 d=(surface-_StationPositions[n].xy)*float2(1,.6);float light=saturate(1-length(d)/_StationPositions[n].z);spill+=_StationColors[n].rgb*light*light;}
                palette+=spill*(.12+value*.25);
                #ifndef UNITY_COLORSPACE_GAMMA
                palette=GammaToLinearSpace(palette);
                #endif
                return fixed4(palette*p.color.rgb*_Color.rgb,source.a*p.color.a*_Color.a);
            }
            ENDCG
        }
    }
}

