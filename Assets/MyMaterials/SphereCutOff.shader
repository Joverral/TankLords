Shader "Custom/sphereCutOff" {
	Properties {
		_Color ("Color", Color) = (1,1,1,1)
		_MainTex ("Albedo (RGB)", 2D) = "white" {}
		_Glossiness ("Smoothness", Range(0,1)) = 0.5
		_AlphaCut("Cutoff" , Range(0,1)) = .4
		_Metallic ("Metallic", Range(0,1)) = 0.0
		_Sphere ("Sphere Data", Vector) = (0.5,3.0,3.0,10.0)
	}
	SubShader 
    {
		//Tags { "Queue" = "AlphaTest" "RenderType" = "TransparentCutout" }
        
        Tags {"Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent"}

		LOD 200
		Cull Off
		//Cull Off
		//CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		//#pragma surface surf Standard noshadow nometa  alphatest:_AlphaCut
        CGPROGRAM #pragma surface surf Standard alpha noshadow nometa keepalpha

		// Use shader model 3.0 target, to get nicer looking lighting
	//	#pragma target 3.0

		sampler2D _MainTex;
		float4 _Sphere;
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

			// Albedo comes from a texture tinted by color
			fixed4 c = tex2D (_MainTex, IN.uv_MainTex) * _Color;
			o.Albedo = c.rgb;

			// Metallic and smoothness come from slider variables
			o.Metallic = _Metallic;
			o.Smoothness = _Glossiness;
            
            
            float3 camD = IN.worldPos - _WorldSpaceCameraPos;
            clip( -dot(camD, IN.worldNormal) );
           // if( length( IN.worldPos -_Sphere.xyz)<_Sphere.w )
          //  {
               //o.Alpha = 0;
          //     clip(-1);
          //  }
          //  if( _WorldSpaceCameraPos)
          //  else
            {
                o.Alpha = c.a;
            }

           //o.Alpha = 0.0;

			//o.Alpha =   length( IN.worldPos -_Sphere.xyz)>_Sphere.w ;
		}
		ENDCG
	}
	
    Fallback "Transparent/VertexLit" 
}