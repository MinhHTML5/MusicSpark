﻿Shader "Custom/LightRay" {
    Properties {
        // Adds Color field we can modify
        _Color ("Main Color", Color) = (1, 1, 1, 1)        
        _MainTex ("Base (RGB)", 2D) = "white" {}
    }

    SubShader {
        Tags { "Queue"="Transparent+3" "IgnoreProjector"="True" "RenderType"="Transparent" }
		Blend SrcAlpha OneMinusSrcAlpha
		ZWrite Off
        ZTest Always
        LOD 100
        
        Pass {
            Lighting Off
            
            SetTexture [_MainTex] { 
                // Sets our color as the 'constant' variable
                constantColor [_Color]
                
                // Multiplies color (in constant) with texture
                combine constant * texture
            }
        }
    }
}