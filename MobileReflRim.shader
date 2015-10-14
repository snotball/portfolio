Shader "RA/Mobile ReflSphere Rim" {
	Properties {
		_MainTex ("Base (RGB) RefStrength (A)", 2D) = "white" {}
		_RimColor ("Rim Color", Color) = (1,1,1,0)
		_RimPower ("Rim Power", Float) = 3.0
		_ReflSphere ("Reflection Sphere", 2D) = "black" {}
	}
	SubShader {
		Tags { "RenderType"="Opaque" }
		LOD 150

		CGPROGRAM
		#pragma surface surf WrapLambert exclude_path:prepass noforwardadd nolightmap

		fixed4 LightingWrapLambert(SurfaceOutput s, fixed3 lightDir, fixed atten) {
			fixed NdotL = dot (s.Normal, lightDir);
			fixed diff = NdotL * 0.5 + 0.5;
			fixed4 c;
			c.rgb = s.Albedo * _LightColor0.rgb * (diff * atten * 2);
			c.a = s.Alpha;
			return c;
		}

		sampler2D _MainTex;
		fixed3 _RimColor;
		fixed _RimPower;
		sampler2D _ReflSphere;

		struct Input {
			fixed2 uv_MainTex;
			fixed3 viewDir;
		};

		void surf (Input IN, inout SurfaceOutput o) {
			fixed4 tex = tex2D(_MainTex, IN.uv_MainTex);
			o.Albedo = tex.rgb;
			
			fixed3 vecX = cross(normalize(IN.viewDir), fixed3(0, 1, 0));
			fixed3 vecY = cross(normalize(IN.viewDir), vecX);
			
			fixed2 vn;
			vn.x = dot(vecX, o.Normal);
			vn.y = dot(-vecY, o.Normal);
			
			fixed NdotE = saturate(dot(normalize(IN.viewDir), o.Normal));
			fixed rim = (1.0 - NdotE) * (o.Normal.y + 1);
			
			fixed3 refl = tex2D(_ReflSphere, vn * 0.5 + 0.5);
			o.Emission = _RimColor.rgb * pow(rim, _RimPower) + refl.rgb * tex.a;
		}
		
		ENDCG
	}
	
	Fallback "Mobile/VertexLit"
}
