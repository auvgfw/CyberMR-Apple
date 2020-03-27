Shader "Mvx2/MeshTextured" {
  Properties{
    _MainTex("Main Texture", 2D) = "white" {}
  }

    SubShader{
    Pass{
      Cull Off

      CGPROGRAM
      #pragma target 2.0
      #pragma vertex vert
      #pragma fragment frag

      #include "UnityCG.cginc"

      uniform sampler2D _MainTex;
      float4 _MainTex_ST;
      int _TextureType;

      struct v2f {
        float4  pos : SV_POSITION;
        float2  uv : TEXCOORD0;
      };

      v2f vert(appdata_base v)
      {
        v2f o;

        o.pos = UnityObjectToClipPos(v.vertex);
        o.uv = TRANSFORM_TEX(v.texcoord, _MainTex);

        return o;
      }

      float4 tex2DNVX(sampler2D texture2d, float2 UV)
      {
        float inv = 1.0 / 1.5;

        float Y = tex2D(texture2d, UV * float2(1.0, inv)).a;
        float U = tex2D(texture2d, float2(0.0, inv) + UV * float2(0.5, 0.5 * inv)).a;
        float V = tex2D(texture2d, float2(0.5, inv) + UV * float2(0.5, 0.5 * inv)).a;

        float R = 1.164*(Y - 16.0 / 256.0) + 1.596*(V - 128.0 / 256.0);
        float G = 1.164*(Y - 16.0 / 256.0) - 0.813*(V - 128.0 / 256.0) - 0.391*(U - 128.0 / 256.0);
        float B = 1.164*(Y - 16.0 / 256.0) + 2.018*(U - 128.0 / 256.0);

        return float4(R, G, B, 1.0);
      }

      float4 frag(v2f i) : COLOR
      {
        if (_TextureType == 0) //NVX
          return tex2DNVX(_MainTex, i.uv);
        else //RGB, ETC2, DXT1, ASTC
          return tex2D(_MainTex, i.uv);
      }
        ENDCG
    }
  }
}
