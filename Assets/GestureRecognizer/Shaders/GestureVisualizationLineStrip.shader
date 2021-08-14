Shader "Gesture/GestureVisualizationLineStrip"
{
    Properties
    {
        _LineThickness("Line Thickness (cm)", Float) = 5.0
        _Color("Gesture Color", Color) = (0, 0, 1, 1)
		_GestureCompleteness("Gesture Completeness", Float) = 1.0
		_GestureTrail("Gesture Completeness", Float) = 0.1
		_GestureGlobalAlpha("Gesture Global Alpha", Float) = 1.0
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent"  }
        LOD 100
		Cull Off

	   Pass
		{
			ZWrite Off
			ColorMask RGB
			Blend SrcAlpha OneMinusSrcAlpha
			Offset -1, -1

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
				float3 normal : NORMAL;
			};

			struct v2g
			{
				float4 worldPos : SV_POSITION;
				float3 normal : NORMAL;
				float4 vertex : UnchangedVertex;
				float2 uv : TEXCOORD0;
				float depth : depth;
			};

			float _LineThickness;
			float _GestureCompleteness;
			float _GestureTrail;
			float _GestureGlobalAlpha;
			float4 _Color;
			bool _Octo3DVisualization;

			struct g2f
			{
				float4 worldPos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float depth : depth;
			};

			[maxvertexcount(12)]
			void geom(lineadj v2g input[4], inout TriangleStream<g2f> triStream)
			{
				g2f o;

				//float3 perp1 = normalize(cross(float3(input[0].vertex.xyz - input[1].vertex.xyz), float3(input[2].vertex.xyz - input[1].vertex.xyz)));
				//float3 perp2 = normalize(cross(float3(input[1].vertex.xyz - input[2].vertex.xyz), float3(input[3].vertex.xyz - input[2].vertex.xyz)));
				float lineWidth = _LineThickness * 0.5f;

				float3 originPos1 = input[0].normal - input[0].vertex.xyz;
				float3 n1 = normalize(float3(-originPos1.y / originPos1.x, 1, 0));

				float4 originPos2 = input[0].vertex - input[1].vertex;
				float3 n2 = normalize(float3(-originPos2.y / originPos2.x, 1, 0));

				if (length(n1 + n2) < 1.2)
				{
					n2 = -n2;
				}

				// First Half
				// 1
				int i = 0;

				o.worldPos = input[i].worldPos;
				o.uv = input[i].uv;
				o.depth = input[i].depth;
				triStream.Append(o);

				o.worldPos = UnityObjectToClipPos(input[0].vertex + float4(n1 * lineWidth * 0.001, 0));
				o.uv = input[i].uv;
				o.depth = input[i].depth;
				triStream.Append(o);

				o.worldPos = input[i + 1].worldPos;
				o.uv = input[i + 1].uv;
				o.depth = input[i + 1].depth;
				triStream.Append(o);

				triStream.RestartStrip();
				

				//2
				o.worldPos = input[i].worldPos;
				o.uv = input[i].uv;
				o.depth = input[i].depth;
				triStream.Append(o);

				o.worldPos = input[i + 1].worldPos;
				o.uv = input[i + 1].uv;
				o.depth = input[i + 1].depth;
				triStream.Append(o);

				o.worldPos = UnityObjectToClipPos(input[0].vertex + float4(-n1 * lineWidth * 0.001, 0));
				o.uv = input[i].uv;
				o.depth = input[i].depth;
				triStream.Append(o);

				triStream.RestartStrip();


				// Second Half
				i++;

				// 3
				o.worldPos = input[i].worldPos;
				o.uv = input[i].uv;
				o.depth = input[i].depth;
				triStream.Append(o);

				o.worldPos = UnityObjectToClipPos(input[i - 1].vertex + float4(n1 * lineWidth * 0.001, 0));
				o.uv = input[i - 1].uv;
				o.depth = input[i - 1].depth;
				triStream.Append(o);

				o.worldPos = UnityObjectToClipPos(input[i].vertex + float4(n2 * lineWidth * 0.001, 0));
				o.uv = input[i].uv;
				o.depth = input[i].depth;
				triStream.Append(o);

				triStream.RestartStrip();
				

				// 4
				o.worldPos = input[i].worldPos;
				o.uv = input[i].uv;
				o.depth = input[i].depth;
				triStream.Append(o);

				o.worldPos = UnityObjectToClipPos(input[i].vertex + float4(-n2 * lineWidth * 0.001, 0));
				o.uv = input[i].uv;
				o.depth = input[i].depth;
				triStream.Append(o);

				o.worldPos = UnityObjectToClipPos(input[i - 1].vertex + float4(-n1 * lineWidth * 0.001, 0));
				o.uv = input[i - 1].uv;
				o.depth = input[i - 1].depth;
				triStream.Append(o);


				triStream.RestartStrip();
			}


			v2g vert(appdata v)
			{
				v2g o;
				o.vertex = v.vertex;
				o.depth = length(v.vertex);
				o.worldPos = UnityObjectToClipPos(v.vertex);
				o.normal = v.normal;

				o.uv = v.uv;
				return o;
			}

			fixed4 frag(g2f i) : SV_Target
			{
				if (_Octo3DVisualization)
				{
					if (_GestureCompleteness + _GestureTrail < i.uv.x)
					{
						float a = lerp(0, 0.2, _GestureGlobalAlpha);
						return fixed4(_Color.rgb, a);
					}
				}
				else
				{
					if (_GestureCompleteness < i.uv.x)
					{
						if (_GestureCompleteness + _GestureTrail < i.uv.x)
							discard;

						float l = (i.uv.x - _GestureCompleteness) / _GestureTrail;
						float a = lerp(_GestureGlobalAlpha, 0, l);
						return fixed4(_Color.rgb, a);
					}
				}



				return fixed4(_Color.rgb, _GestureGlobalAlpha);// *_Color;
			}
			ENDCG
		}
    }
}
