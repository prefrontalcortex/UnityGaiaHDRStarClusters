﻿Shader "Wireframe/NoCull"
{
	Properties
	{
		_LineColor ("Line color", Color) = (0, 0, 0, 1)
		_LineSize ("Line size", float) = 0.3
		_ScaleSize ("Scale size", float) = 1.0
	}
	
	SubShader
	{
		Tags { "RenderType"="Transparent" "Queue"="Transparent" }
		LOD 100

		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha
			Cull Off
			ZWrite Off

			CGPROGRAM
			#include "UnityCG.cginc"
			#include "WireframeBarycentricCoordinatesCore.cginc"
			#pragma vertex vert
			#pragma fragment frag
			ENDCG
		}
		
	}
}
