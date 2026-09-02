Shader "DrawRush/DropOutline"
{
    // Inverted-hull outline for the corner drops. The drop is a 3D tear-drop that billboards to the
    // camera; this grows a back-face-only copy of the mesh OUTWARD along its world normals, so the
    // rim is genuinely behind the drop in 3D (the drop's front faces cover the centre — no dark
    // bleed) and stays perfectly concentric from every angle. The growth is scaled by the drop's
    // distance to the camera, so the rim is the SAME pixel thickness on near and far drops alike.
    // The "shader that follows the camera" the drops needed so they never vanish against a
    // same-colour wall or ground.
    Properties
    {
        _OutlineColor ("Outline Color", Color) = (0.09, 0.09, 0.13, 1)
        // Thickness ~ fraction of the drop's camera distance (0.006 * distance world units), which
        // projects to a roughly constant on-screen thickness. ~0.006 reads as a clean thin rim.
        _OutlineWidth ("Outline Width", Range(0,0.03)) = 0.006
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        Pass
        {
            Name "Outline"
            Cull Front          // draw the far side of the grown hull -> the drop covers the centre
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; };
            struct Varyings   { float4 positionCS : SV_POSITION; };

            float  _OutlineWidth;
            float4 _OutlineColor;

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                float3 posWS    = TransformObjectToWorld(IN.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(IN.normalOS);   // unit length
                float  dist     = distance(posWS, _WorldSpaceCameraPos);
                posWS += normalWS * (_OutlineWidth * dist);                    // constant screen size
                OUT.positionCS = TransformWorldToHClip(posWS);
                return OUT;
            }

            half4 frag (Varyings IN) : SV_Target
            {
                return _OutlineColor;
            }
            ENDHLSL
        }
    }
    Fallback Off
}
