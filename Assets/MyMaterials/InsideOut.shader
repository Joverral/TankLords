Shader "Custom/InsideOut" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_Metallic ("Metallic", Range(0,1)) = 0.0
	}
	SubShader 
    {
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}

		LOD 200
		Cull Off
        CGPROGRAM #pragma surface surf Standard alpha noshadow nometa keepalpha

		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 3.0

		sampler2D _MainTex;
		struct Input {
			float2 uv_MainTex;
			float3 worldPos;
            float3 worldNormal;
		};

		half _Glossiness;
		half _Metallic;
		fixed4 _Color;

		void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float3 camD = IN.worldPos - _WorldSpaceCameraPos;
            clip( -dot(camD, IN.worldNormal) );
            
			// Albedo comes from a texture tinted by color
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb;
            o.Alpha = c.a;
            
			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;

		}
		ENDCG
	}
	
    Fallback "Transparent/VertexLit" 
}