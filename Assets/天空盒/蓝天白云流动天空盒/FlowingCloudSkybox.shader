Shader "Skybox/Flowing Blue Sky Clouds Procedural"
{
    Properties
    {
        _ZenithColor ("Zenith Blue", Color) = (0.17, 0.48, 0.95, 1)
        _HorizonColor ("Horizon Blue", Color) = (0.66, 0.88, 1.0, 1)
        _GroundTint ("Lower Sky Tint", Color) = (0.78, 0.92, 1.0, 1)
        _CloudColor ("Cloud Color", Color) = (1.0, 0.98, 0.92, 1)
        _CloudShadowColor ("Cloud Shadow", Color) = (0.58, 0.72, 0.88, 1)
        [HDR] _SunColor ("Sun Glow", Color) = (1.45, 1.22, 0.72, 1)
        _Exposure ("Exposure", Range(0, 3)) = 1.05
        _CloudCoverage ("Cloud Coverage", Range(0, 1)) = 0.46
        _CloudSoftness ("Cloud Softness", Range(0.01, 0.5)) = 0.18
        _CloudSpeed ("Cloud Speed", Range(0, 3)) = 0.32
        _CloudScale ("Cloud Scale", Range(0.5, 8)) = 2.6
        _WispyClouds ("Wispy Clouds", Range(0, 2)) = 0.85
        _SunIntensity ("Sun Intensity", Range(0, 5)) = 1.6
        _Rotation ("Rotation", Range(0, 360)) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Cull Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 dir : TEXCOORD0;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float4 _ZenithColor;
            float4 _HorizonColor;
            float4 _GroundTint;
            float4 _CloudColor;
            float4 _CloudShadowColor;
            float4 _SunColor;
            float _Exposure;
            float _CloudCoverage;
            float _CloudSoftness;
            float _CloudSpeed;
            float _CloudScale;
            float _WispyClouds;
            float _SunIntensity;
            float _Rotation;

            float hash31(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 33.33);
                return frac((p.x + p.y) * p.z);
            }

            float noise3(float3 p)
            {
                float3 i = floor(p);
                float3 f = frac(p);
                f = f * f * (3.0 - 2.0 * f);

                float n000 = hash31(i + float3(0, 0, 0));
                float n100 = hash31(i + float3(1, 0, 0));
                float n010 = hash31(i + float3(0, 1, 0));
                float n110 = hash31(i + float3(1, 1, 0));
                float n001 = hash31(i + float3(0, 0, 1));
                float n101 = hash31(i + float3(1, 0, 1));
                float n011 = hash31(i + float3(0, 1, 1));
                float n111 = hash31(i + float3(1, 1, 1));

                float nx00 = lerp(n000, n100, f.x);
                float nx10 = lerp(n010, n110, f.x);
                float nx01 = lerp(n001, n101, f.x);
                float nx11 = lerp(n011, n111, f.x);
                float nxy0 = lerp(nx00, nx10, f.y);
                float nxy1 = lerp(nx01, nx11, f.y);
                return lerp(nxy0, nxy1, f.z);
            }

            float fbm(float3 p)
            {
                float value = 0.0;
                float amp = 0.52;
                [unroll]
                for (int i = 0; i < 5; i++)
                {
                    value += noise3(p) * amp;
                    p = p * 2.03 + float3(13.7, 9.2, 5.4);
                    amp *= 0.5;
                }
                return value;
            }

            float3 rotateY(float3 p, float radians)
            {
                float s = sin(radians);
                float c = cos(radians);
                return float3(c * p.x - s * p.z, p.y, s * p.x + c * p.z);
            }

            v2f vert(appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.dir = v.vertex.xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float time = _Time.y * _CloudSpeed;
                float3 dir = normalize(i.dir);
                dir = rotateY(dir, radians(_Rotation));

                float vertical = saturate(dir.y * 0.5 + 0.5);
                float horizon = pow(1.0 - abs(dir.y), 2.2);
                float3 sky = lerp(_GroundTint.rgb, _ZenithColor.rgb, smoothstep(0.0, 1.0, vertical));
                sky = lerp(sky, _HorizonColor.rgb, horizon * 0.65);

                float3 sunDir = normalize(float3(0.35, 0.42, 0.84));
                float sunDot = saturate(dot(dir, sunDir));
                float sunDisc = pow(sunDot, 520.0);
                float sunGlow = pow(sunDot, 14.0);
                sky += _SunColor.rgb * (sunDisc * 2.8 + sunGlow * 0.24) * _SunIntensity;

                float upperMask = smoothstep(-0.08, 0.32, dir.y) * (1.0 - smoothstep(0.88, 1.0, dir.y));
                float horizonCloudMask = smoothstep(-0.16, 0.12, dir.y) * (1.0 - smoothstep(0.72, 0.98, dir.y));

                float3 cloudP = float3(dir.x, dir.y * 0.42, dir.z) * _CloudScale;
                cloudP += float3(time * 0.32, time * 0.035, -time * 0.18);
                float puffy = fbm(cloudP);
                float detail = fbm(cloudP * 2.7 + float3(-time * 0.18, time * 0.05, time * 0.27));
                float cloudField = puffy * 0.72 + detail * 0.28;
                float threshold = lerp(0.82, 0.42, _CloudCoverage);
                float clouds = smoothstep(threshold, threshold + _CloudSoftness, cloudField) * upperMask;

                float3 wispyP = rotateY(dir, 0.65) * (_CloudScale * 3.8) + float3(-time * 0.55, time * 0.02, time * 0.14);
                float wisps = fbm(wispyP);
                float streaks = sin((atan2(dir.x, dir.z) + time * 0.08) * 18.0 + wisps * 4.0);
                wisps = smoothstep(0.48, 0.82, wisps) * smoothstep(0.15, 0.95, 1.0 - abs(streaks) * 0.35);
                wisps *= horizonCloudMask * _WispyClouds;

                float lighting = saturate(0.55 + dot(dir, sunDir) * 0.45);
                float3 cloudLit = lerp(_CloudShadowColor.rgb, _CloudColor.rgb, lighting);
                cloudLit += _SunColor.rgb * pow(sunDot, 9.0) * 0.16;
                sky = lerp(sky, cloudLit, saturate(clouds * 0.92));
                sky = lerp(sky, lerp(_CloudShadowColor.rgb, _CloudColor.rgb, 0.78), saturate(wisps * 0.42));

                float distantHaze = horizon * smoothstep(-0.25, 0.2, dir.y) * 0.22;
                sky = lerp(sky, _HorizonColor.rgb, distantHaze);

                sky *= _Exposure;
                sky = 1.0 - exp(-sky);
                return fixed4(sky, 1.0);
            }
            ENDCG
        }
    }

    FallBack Off
}
