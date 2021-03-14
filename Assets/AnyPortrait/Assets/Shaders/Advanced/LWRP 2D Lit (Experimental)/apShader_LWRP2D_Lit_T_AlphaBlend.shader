/*
*	Copyright (c) 2017-2019. RainyRizzle. All rights reserved
*	Contact to : https://www.rainyrizzle.com/ , contactrainyrizzle@gmail.com
*
*	This file is part of [AnyPortrait].
*
*	AnyPortrait can not be copied and/or distributed without
*	the express perission of [Seungjik Lee].
*
*	Unless this file is downloaded from the Unity Asset Store or RainyRizzle homepage,
*	this file and its users are illegal.
*	In that case, the act may be subject to legal penalties.
*/

Shader "AnyPortrait/Advanced/LWRP 2D Lit (Experimental)/AlphaBlend"
{
	Properties
	{
		[NoScaleOffset] _MainTex("MainTex", 2D) = "white" {}
		_MainTex_ST("MainTex ST", Vector) = (1,1,0,0)
		_Color("Color", Color) = (0.5, 0.5, 0.5, 1)

	}
	
	SubShader
	{
		Tags{ "RenderPipeline" = "LightweightPipeline"}
		Tags
		{
			"RenderPipeline" = "LightweightPipeline"
			"RenderType" = "Transparent"
			"Queue" = "Transparent"
		}
		Pass
		{
			Name "Sprite Lit"
			Tags { "LightMode" = "Lightweight2D" }


			Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
			//Blend One One, One One
			//Blend DstColor SrcColor, One OneMinusSrcAlpha//2X Multiply
			//Blend OneMinusDstColor One, One OneMinusSrcAlpha//Soft Add

			//Cull Off
			//ZWrite Off

			HLSLPROGRAM
	
			// Required to compile gles 2.0 with standard srp library
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma target 2.0

			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile _ ETC1_EXTERNAL_ALPHA
			#pragma multi_compile USE_SHAPE_LIGHT_TYPE_0 __
			#pragma multi_compile USE_SHAPE_LIGHT_TYPE_1 __
			#pragma multi_compile USE_SHAPE_LIGHT_TYPE_2 __
			#pragma multi_compile USE_SHAPE_LIGHT_TYPE_3 __

			#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.lightweight/Shaders/2D/Include/LightingUtility.hlsl"

			#if ETC1_EXTERNAL_ALPHA
				TEXTURE2D(_AlphaTex); SAMPLER(sampler_AlphaTex);
				float _EnableAlphaTexture;
			#endif

			#if USE_SHAPE_LIGHT_TYPE_0
			SHAPE_LIGHT(0)
			#endif

			#if USE_SHAPE_LIGHT_TYPE_1
			SHAPE_LIGHT(1)
			#endif

			#if USE_SHAPE_LIGHT_TYPE_2
			SHAPE_LIGHT(2)
			#endif

			#if USE_SHAPE_LIGHT_TYPE_3
			SHAPE_LIGHT(3)
			#endif

			#include "Packages/com.unity.render-pipelines.lightweight/Shaders/2D/Include/CombinedShapeLightShared.hlsl"

			CBUFFER_START(UnityPerMaterial)
			float4 _MainTex_ST;
			float4 _Color;
			CBUFFER_END

			TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex); float4 _MainTex_TexelSize;
			SAMPLER(_SampleTexture2D_A48C5A7A_Sampler_3_Linear_Repeat);
			
			struct VertexDescriptionInputs
			{
				float3 ObjectSpacePosition;
			};

			struct SurfaceDescriptionInputs
			{
				half4 uv0;
			};


			void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
			{
				Out = UV * Tiling + Offset;
			}

			struct VertexDescription
			{
				float3 Position;
			};

			VertexDescription PopulateVertexData(VertexDescriptionInputs IN)
			{
				VertexDescription description = (VertexDescription)0;
				description.Position = IN.ObjectSpacePosition;
				return description;
			}

			struct SurfaceDescription
			{
				float4 Color;
				float4 Mask;
			};

			SurfaceDescription PopulateSurfaceData(SurfaceDescriptionInputs IN)
			{
				SurfaceDescription surface = (SurfaceDescription)0;
				
				float2 UV;
				Unity_TilingAndOffset_float(IN.uv0.xy, float2(_MainTex_ST.x, _MainTex_ST.y), float2(_MainTex_ST.z, _MainTex_ST.w), UV);
				
				float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, UV);
				
				texColor.rgb = IsGammaSpace() ? (texColor.rgb * _Color.rgb * 2.0f) : (pow(saturate(texColor.rgb * _Color.rgb * 4.595f), 2.2f));
				texColor.a *= _Color.a;

				surface.Color = texColor;
				surface.Mask = IsGammaSpace() ? float4(1, 1, 1, 1) : float4 (SRGBToLinear(float3(1, 1, 1)), 1);
				return surface;
			}

			struct GraphVertexInput
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
				float4 texcoord0 : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};


			struct GraphVertexOutput
			{
				float4 positionCS : POSITION;
				half2  lightingUV : TEXCOORD0;
				float4 VertexColor : COLOR;
				half4 uv0 : TEXCOORD3;

			};

			GraphVertexOutput vert(GraphVertexInput v)
			{
				GraphVertexOutput o = (GraphVertexOutput)0;
				float3 WorldSpacePosition = mul(UNITY_MATRIX_M,v.vertex).xyz;
				float4 VertexColor = v.color;
				float4 uv0 = v.texcoord0;
				float3 ObjectSpacePosition = mul(UNITY_MATRIX_I_M,float4(WorldSpacePosition,1.0)).xyz;

				VertexDescriptionInputs vdi = (VertexDescriptionInputs)0;
				vdi.ObjectSpacePosition = ObjectSpacePosition;

				VertexDescription vd = PopulateVertexData(vdi);

				v.vertex.xyz = vd.Position;
				VertexColor = v.color;

				o.positionCS = TransformObjectToHClip(v.vertex.xyz);
				float4 clipVertex = o.positionCS / o.positionCS.w;
				o.lightingUV = ComputeScreenPos(clipVertex).xy;

				#if UNITY_UV_STARTS_AT_TOP
				o.lightingUV.y = 1.0 - o.lightingUV.y;
				#endif

				o.VertexColor = VertexColor;
				o.uv0 = uv0;

				return o;
			}

			half4 frag(GraphVertexOutput IN) : SV_Target
			{
				float4 VertexColor = IN.VertexColor;
				float4 uv0 = IN.uv0;

				SurfaceDescriptionInputs surfaceInput = (SurfaceDescriptionInputs)0;
				surfaceInput.uv0 = uv0;

				SurfaceDescription surf = PopulateSurfaceData(surfaceInput);

				#if ETC1_EXTERNAL_ALPHA
				float4 alpha = SAMPLE_TEXTURE2D(_AlphaTex, sampler_AlphaTex, IN.uv0.xy);
				surf.Color.a = lerp(surf.Color.a, alpha.r, _EnableAlphaTexture);
				#endif
				
				surf.Color *= IN.VertexColor;

				return CombinedShapeLightShared(surf.Color, surf.Mask, IN.lightingUV);
			}

			ENDHLSL
		}

		Pass
		{
			Name "Sprite Normal"
			Tags { "LightMode" = "NormalsRendering" }

			Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
			
			//Cull Off
			//ZWrite Off

			HLSLPROGRAM
			// Required to compile gles 2.0 with standard srp library
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma target 2.0

			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile _ ETC1_EXTERNAL_ALPHA

			#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
			#include "Packages/com.unity.render-pipelines.lightweight/Shaders/2D/Include/NormalsRenderingShared.hlsl"

			#if ETC1_EXTERNAL_ALPHA
				TEXTURE2D(_AlphaTex); SAMPLER(sampler_AlphaTex);
				float _EnableAlphaTexture;
			#endif

			CBUFFER_START(UnityPerMaterial)
			float4 _MainTex_ST;
			float4 _Color;
			CBUFFER_END

			TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex); float4 _MainTex_TexelSize;
			SAMPLER(_SampleTexture2D_A48C5A7A_Sampler_3_Linear_Repeat);
			struct VertexDescriptionInputs
			{
				float3 ObjectSpacePosition;
			};

			struct SurfaceDescriptionInputs
			{
				half4 uv0;
			};

			void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
			{
				Out = UV * Tiling + Offset;
			}

			void Unity_Combine_float(float R, float G, float B, float A, out float4 RGBA, out float3 RGB, out float2 RG)
			{
				RGBA = float4(R, G, B, A);
				RGB = float3(R, G, B);
				RG = float2(R, G);
			}

			void Unity_Multiply_float(float3 A, float3 B, out float3 Out)
			{
				Out = A * B;
			}

			void Unity_Multiply_float(float A, float B, out float Out)
			{
				Out = A * B;
			}

			struct VertexDescription
			{
				float3 Position;
			};

			VertexDescription PopulateVertexData(VertexDescriptionInputs IN)
			{
				VertexDescription description = (VertexDescription)0;
				description.Position = IN.ObjectSpacePosition;
				return description;
			}

			struct SurfaceDescription
			{
				float4 Color;
				float3 Normal;
			};

			SurfaceDescription PopulateSurfaceData(SurfaceDescriptionInputs IN)
			{
				SurfaceDescription surface = (SurfaceDescription)0;
			
				float2 UV;
				Unity_TilingAndOffset_float(IN.uv0.xy, float2(_MainTex_ST.x, _MainTex_ST.y), float2(_MainTex_ST.z, _MainTex_ST.w), UV);

				float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, UV);

				texColor.rgb = IsGammaSpace() ? (texColor.rgb * _Color.rgb * 2.0f) : (pow(saturate(texColor.rgb * _Color.rgb * 4.595f), 2.2f));
				texColor.a *= _Color.a;

				surface.Color = texColor;
				surface.Normal = float3 (0, 0, 1);
				return surface;
			}

			struct GraphVertexInput
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
				float4 texcoord0 : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};


			struct GraphVertexOutput
			{
				float4	position		: POSITION;
				float3  normalWS		: TEXCOORD0;
				float3  tangentWS		: TEXCOORD1;
				float3  bitangentWS		: TEXCOORD2;
				float4 VertexColor : COLOR;
				half4 uv0 : TEXCOORD3;

			};

			GraphVertexOutput vert(GraphVertexInput v)
			{
				GraphVertexOutput o = (GraphVertexOutput)0;
				float3 WorldSpacePosition = mul(UNITY_MATRIX_M,v.vertex).xyz;
				float4 VertexColor = v.color;
				float4 uv0 = v.texcoord0;
				float3 ObjectSpacePosition = mul(UNITY_MATRIX_I_M,float4(WorldSpacePosition,1.0)).xyz;

				VertexDescriptionInputs vdi = (VertexDescriptionInputs)0;
				vdi.ObjectSpacePosition = ObjectSpacePosition;

				VertexDescription vd = PopulateVertexData(vdi);

				v.vertex.xyz = vd.Position;
				o.position = TransformObjectToHClip(v.vertex.xyz);
				#if UNITY_UV_STARTS_AT_TOP
					o.position.y = -o.position.y;
				#endif
				o.normalWS = TransformObjectToWorldDir(float3(0, 0, 1));
				o.tangentWS = TransformObjectToWorldDir(float3(1, 0, 0));
				o.bitangentWS = TransformObjectToWorldDir(float3(0, 1, 0));
				o.VertexColor = VertexColor;
				o.uv0 = uv0;

				return o;
			}

			half4 frag(GraphVertexOutput IN) : SV_Target
			{
				float4 VertexColor = IN.VertexColor;
				float4 uv0 = IN.uv0;

				SurfaceDescriptionInputs surfaceInput = (SurfaceDescriptionInputs)0;
				surfaceInput.uv0 = uv0;

				SurfaceDescription surf = PopulateSurfaceData(surfaceInput);

				return NormalsRenderingShared(surf.Color, surf.Normal, IN.tangentWS.xyz, IN.bitangentWS.xyz, -IN.normalWS.xyz);;
			}

			ENDHLSL
		}

		Pass
		{
			Name "Sprite Forward"
			Tags{"LightMode" = "LightweightForward"}

			Blend SrcAlpha OneMinusSrcAlpha, One OneMinusSrcAlpha
			
			//Cull Off
			//ZWrite OFf

			HLSLPROGRAM
			// Required to compile gles 2.0 with standard srp library
			#pragma prefer_hlslcc gles
			#pragma exclude_renderers d3d11_9x
			#pragma target 2.0

			#pragma vertex vert
			#pragma fragment frag

			#pragma multi_compile _ ETC1_EXTERNAL_ALPHA


			#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Core.hlsl"
			#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/Lighting.hlsl"
			#include "Packages/com.unity.render-pipelines.lightweight/ShaderLibrary/ShaderGraphFunctions.hlsl"
			#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

			#if ETC1_EXTERNAL_ALPHA
				TEXTURE2D(_AlphaTex); SAMPLER(sampler_AlphaTex);
				float _EnableAlphaTexture;
			#endif
			float4 _RendererColor;

			CBUFFER_START(UnityPerMaterial)
			float4 _MainTex_ST;
			float4 _Color;
			CBUFFER_END

			TEXTURE2D(_MainTex); SAMPLER(sampler_MainTex); float4 _MainTex_TexelSize;
			SAMPLER(_SampleTexture2D_A48C5A7A_Sampler_3_Linear_Repeat);
			struct VertexDescriptionInputs
			{
				float3 ObjectSpacePosition;
			};

			struct SurfaceDescriptionInputs
			{
				half4 uv0;
			};


			void Unity_TilingAndOffset_float(float2 UV, float2 Tiling, float2 Offset, out float2 Out)
			{
				Out = UV * Tiling + Offset;
			}

			

			struct VertexDescription
			{
				float3 Position;
			};

			VertexDescription PopulateVertexData(VertexDescriptionInputs IN)
			{
				VertexDescription description = (VertexDescription)0;
				description.Position = IN.ObjectSpacePosition;
				return description;
			}

			struct SurfaceDescription
			{
				float4 Color;
				float3 Normal;
			};

			SurfaceDescription PopulateSurfaceData(SurfaceDescriptionInputs IN)
			{
				SurfaceDescription surface = (SurfaceDescription)0;
				
				float2 UV;
				Unity_TilingAndOffset_float(IN.uv0.xy, float2(_MainTex_ST.x, _MainTex_ST.y), float2(_MainTex_ST.z, _MainTex_ST.w), UV);

				float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, UV);

				texColor.rgb = IsGammaSpace() ? (texColor.rgb * _Color.rgb * 2.0f) : (pow(saturate(texColor.rgb * _Color.rgb * 4.595f), 2.2f));
				texColor.a *= _Color.a;

				surface.Color = texColor;
				surface.Normal = float3 (0, 0, 1);
				return surface;
			}

			struct GraphVertexInput
			{
				float4 vertex : POSITION;
				float4 color : COLOR;
				float4 texcoord0 : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};


			struct GraphVertexOutput
			{
				float4 position : POSITION;
				float4 VertexColor : COLOR;
				half4 uv0 : TEXCOORD3;

			};

			GraphVertexOutput vert(GraphVertexInput v)
			{
				GraphVertexOutput o = (GraphVertexOutput)0;
				float3 WorldSpacePosition = mul(UNITY_MATRIX_M,v.vertex).xyz;
				float4 VertexColor = v.color;
				float4 uv0 = v.texcoord0;
				float3 ObjectSpacePosition = mul(UNITY_MATRIX_I_M,float4(WorldSpacePosition,1.0)).xyz;

				VertexDescriptionInputs vdi = (VertexDescriptionInputs)0;
				vdi.ObjectSpacePosition = ObjectSpacePosition;

				VertexDescription vd = PopulateVertexData(vdi);

				v.vertex.xyz = vd.Position;
				VertexColor = v.color;
				o.position = TransformObjectToHClip(v.vertex.xyz);

				o.VertexColor = VertexColor;
				o.uv0 = uv0;

				return o;
			}

			half4 frag(GraphVertexOutput IN) : SV_Target
			{
				float4 VertexColor = IN.VertexColor;
				float4 uv0 = IN.uv0;

				SurfaceDescriptionInputs surfaceInput = (SurfaceDescriptionInputs)0;
				surfaceInput.uv0 = uv0;

				SurfaceDescription surf = PopulateSurfaceData(surfaceInput);

				#if ETC1_EXTERNAL_ALPHA
				float4 alpha = SAMPLE_TEXTURE2D(_AlphaTex, sampler_AlphaTex, IN.uv0.xy);
				surf.Color.a = lerp(surf.Color.a, alpha.r, _EnableAlphaTexture);
				#endif

				surf.Color *= IN.VertexColor;
				return surf.Color;
			}

			ENDHLSL
		}
}
FallBack "Hidden/InternalErrorShader"
}
