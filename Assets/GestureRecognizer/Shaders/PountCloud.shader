Shader "Custom/PointCloudGeom" {
	Properties{
		[NoScaleOffset]_MainTex("Texture", 2D) = "white" {}
		[NoScaleOffset]_UVMap("UV", 2D) = "white" {}
		_PointSize("Point Size", Float) = 4.0
		_Color("PointCloud Color", Color) = (1, 1, 1, 1)
		[Toggle(USE_DISTANCE)]_UseDistance("Scale by distance?", float) = 0
	}

		SubShader
		{
			Cull Off
			Pass
			{
				CGPROGRAM
				#pragma vertex vert
				#pragma geometry geom
				#pragma fragment frag
				#pragma shader_feature USE_DISTANCE
				#include "UnityCG.cginc"

				struct appdata
				{
					float4 vertex : POSITION;
					float2 uv : TEXCOORD0;
				};

				struct v2f
				{
					float4 vertex : SV_POSITION;
					float2 uv : TEXCOORD0;
					float depth : depth;
				};

				float _PointSize;
				float4 _Color;
				sampler2D _MainTex;
				float4 _MainTex_TexelSize;

				sampler2D _UVMap;
				float4 _UVMap_TexelSize;


				struct g2f
				{
					float4 vertex : SV_POSITION;
					float2 uv : TEXCOORD0;
					float depth : depth;
					float3 barycoord : barycoord;
				};

				[maxvertexcount(3)]
				void geom(triangle v2f input[3], inout TriangleStream<g2f> triStream)
				{
					g2f output;

					[unroll]
					for (int i = 0; i < 3; ++i)
					{
						output.vertex = input[i].vertex;
						output.uv = input[i].uv;
						output.depth = input[i].depth;

						// BarryCentric Coordinates
						float3 coords = float3(0, 0, 0);
						coords[i] = 1;
						output.barycoord = coords;

						triStream.Append(output);
					}
					triStream.RestartStrip();
				}


				v2f vert(appdata v)
				{
					v2f o;
					//o.vertex = v.vertex;
					o.depth = length(v.vertex);
					o.vertex = UnityObjectToClipPos(v.vertex);

					o.uv = v.uv;
					return o;
				}

				fixed4 frag(g2f i) : SV_Target
				{
					if (i.depth < 0.2) discard;

					if (false) {
						float minBarryCoord = min(i.barycoord.x, min(i.barycoord.y, i.barycoord.z));
						if (minBarryCoord < 0.05) {
							return float4(0, 0, 0, 1);
						}
					}

					float4 black = float4(0, 0, 0, 1);
					float4 white = float4(1, 1, 1, 1);

					float2 uv = i.uv;

					float height = 1.0 / 20.0;
					height *= 2.0;

					float width = height;

					float boxY = fmod(uv.y, height) / height;
					float boxX = fmod(uv.x, width) / width;

					float4 blend;
					if ((boxX < 0.5 && boxY < 0.5) ||
						(boxX > 0.5 && boxY > 0.5))
					{
						// White
						blend = white;
					}
					else
					{
						// Black
						blend = black;
					}



					return blend;// *_Color;
				}
				ENDCG
			}
		}
}