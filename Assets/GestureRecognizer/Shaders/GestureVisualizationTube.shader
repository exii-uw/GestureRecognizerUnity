// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

Shader "Gesture/GestureVisualizationTube"
{
	Properties
	{
		_LineThickness("Line Thickness (cm)", Float) = 5.0
		_Color("Gesture Color", Color) = (0, 0, 1, 1)
		_GestureCompleteness("Gesture Completeness", Float) = 1.0
		_GestureTrail("Gesture Completeness", Float) = 0.1
		_GestureGlobalAlpha("Gesture Global Alpha", Float) = 1.0

		_SpecColor("Specular Color", Color) = (1,1,1,1)
		_Shininess("Shininess", Float) = 0.2
	}
		SubShader
	{
		Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }
		LOD 100
		Cull Off

	   Pass
		{
			ZWrite On
			ColorMask RGB
			Blend SrcAlpha OneMinusSrcAlpha
			Offset -1, -1

			CGPROGRAM
			#pragma vertex vert
			#pragma geometry geom
			#pragma fragment frag
			#pragma shader_feature USE_DISTANCE
			#include "UnityCG.cginc"
			uniform float4 _LightColor0;

			uniform float4 _SpecColor;
			uniform float _Shininess;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
				float3 normal : NORMAL;
			};

			struct v2g
			{
				float4 pos : SV_POSITION;
				float3 normal : NORMAL;
				float4 vertex : UnchangedVertex;
				float4 worldPos : TEXCOORD1;
				float2 uv : TEXCOORD0;
				float depth : depth;
			};

			float _LineThickness;
			float _GestureCompleteness;
			float _GestureTrail;
			float _GestureGlobalAlpha;
			float4 _Color;

			struct g2f
			{
				float4 pos : SV_POSITION;
				float3 normal : NORMAL;
				float4 worldPos : TEXCOORD1;
				float2 uv : TEXCOORD0;

				float depth : depth;
			};

			[maxvertexcount(24)]
			void geom(lineadj v2g input[4], inout TriangleStream<g2f> triStream)
			{
				g2f o;

				float lineWidth = _LineThickness * 0.5f;

				float3 dir1 = normalize(input[0].normal - input[0].vertex.xyz);
				float3 n1 = normalize(float3(-dir1.y / dir1.x, 1, 0));
				float3 n2 = normalize(cross(n1, dir1));
				float3 n3 = -n1;
				float3 n4 = -n2;

				float4 dir2 = normalize(input[0].vertex - input[1].vertex);
				float3 f1 = normalize(float3(-dir2.y / dir2.x, 1, 0));
				if (length(n1 + f1) < 1.2)
					f1 = -f1;

				float3 f2 = normalize(cross(f1, dir2));
				float3 f3 = -f1;
				float3 f4 = -f2;

				// Set Arrays
				float3 near[4];
				near[0] = n1;
				near[1] = n2;
				near[2] = n3;
				near[3] = n4;

				float3 far[4];
				far[0] = f1;
				far[1] = f2;
				far[2] = f3;
				far[3] = f4;

				int count = 4;


				for (int i = 0; i < count; ++i)
				{
					int j = (i + 1) % count;


					
					float4 objectPos;

					// 1
					//float3 facenormal = normalize(cross(near[i] - near[j], far[j] - near[j]));
					//facenormal = normalize(mul(float4(facenormal, 0.0), unity_ObjectToWorld).xyz);
					float3 facenormal = normalize(near[i]);
					//float3 facenormal = normalize(near[j]);


					objectPos = input[0].vertex + float4(near[i] * lineWidth * 0.001, 0);
					o.pos = UnityObjectToClipPos(objectPos);
					o.uv = input[0].uv;
					o.depth = input[0].depth;
					o.normal = facenormal;
					o.worldPos = mul(objectPos, unity_ObjectToWorld);
					triStream.Append(o);

					objectPos = input[0].vertex + float4(near[j] * lineWidth * 0.001, 0);
					o.pos = UnityObjectToClipPos(objectPos);
					o.uv = input[0].uv;
					o.depth = input[0].depth;
					o.normal = facenormal;
					o.worldPos = mul(objectPos, unity_ObjectToWorld);
					triStream.Append(o);

					objectPos = input[1].vertex + float4(far[j] * lineWidth * 0.001, 0);
					o.pos = UnityObjectToClipPos(objectPos);
					o.uv = input[0].uv;
					o.depth = input[0].depth;
					o.normal = facenormal;
					o.worldPos = mul(objectPos, unity_ObjectToWorld);
					triStream.Append(o);

					triStream.RestartStrip();


					// 2
					//facenormal = normalize(cross(far[j] - near[i], far[i] - near[i]));
					//facenormal = normalize(mul(float4(facenormal, 0.0), unity_ObjectToWorld).xyz);

					objectPos = input[0].vertex + float4(near[i] * lineWidth * 0.001, 0);
					o.pos = UnityObjectToClipPos(objectPos);
					o.uv = input[0].uv;
					o.depth = input[0].depth;
					o.normal = facenormal;
					o.worldPos = mul(objectPos, unity_ObjectToWorld);
					triStream.Append(o);

					objectPos = input[1].vertex + float4(far[j] * lineWidth * 0.001, 0);
					o.pos = UnityObjectToClipPos(objectPos);
					o.uv = input[0].uv;
					o.depth = input[0].depth;
					o.normal = facenormal;
					o.worldPos = mul(objectPos, unity_ObjectToWorld);
					triStream.Append(o);

					objectPos = input[1].vertex + float4(far[i] * lineWidth * 0.001, 0);
					o.pos = UnityObjectToClipPos(objectPos);
					o.uv = input[0].uv;
					o.depth = input[0].depth;
					o.normal = facenormal;
					o.worldPos = mul(objectPos, unity_ObjectToWorld);
					triStream.Append(o);

					triStream.RestartStrip();
				}

			}


			v2g vert(appdata v)
			{
				v2g o;
				o.vertex = v.vertex;
				o.depth = length(v.vertex);
				o.pos = UnityObjectToClipPos(v.vertex);
				o.worldPos = mul(v.vertex, unity_ObjectToWorld);
				o.normal = v.normal;

				o.uv = v.uv;
				return o;
			}

			fixed4 frag(g2f i) : SV_Target
			{
				float3 normalDirection = normalize(i.normal);
				float3 viewDirection = normalize(_WorldSpaceCameraPos - i.worldPos.xyz);
				float3 lightDirection;
				float attenuation;

				attenuation = 1.0; // no attenuation
				lightDirection = normalize(_WorldSpaceLightPos0.xyz);

				float3 ambientLighting =
					UNITY_LIGHTMODEL_AMBIENT.rgb * _Color.rgb;

				float3 diffuseReflection =
					attenuation * _LightColor0.rgb * _Color.rgb
					* max(0.0, dot(normalDirection, lightDirection));



				float3 specularReflection;
				if (dot(normalDirection, lightDirection) < 0.0)
				// light source on the wrong side?
				{
					specularReflection = float3(0.0, 0.0, 0.0); // no specular reflection
				}
				else
				// light source on the right side
				{
					specularReflection =
					attenuation *
					_LightColor0.rgb *
					_SpecColor.rgb *
					pow(max(0.0, dot(reflect(-lightDirection, normalDirection),viewDirection)),
					_Shininess)
						// terminator optimization
						* dot(lightDirection, normalDirection);
				}


				float4 final = float4(ambientLighting + diffuseReflection + specularReflection, 1.0);

				if (_GestureCompleteness < i.uv.x)
				{
					if (_GestureCompleteness + _GestureTrail < i.uv.x)
						discard;

					float l = (i.uv.x - _GestureCompleteness) / _GestureTrail;
					float a = lerp(_GestureGlobalAlpha, 0, l);
					return fixed4(final.rgb, a);
				}



				return fixed4(final.rgb, _GestureGlobalAlpha);// *_Color;
			}
			ENDCG
		}
	
		
	}
}
