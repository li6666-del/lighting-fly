Shader "Skybox/Thunder Nebula Procedural"
{
    Properties
    {
        _TopColor ("Top Color", Color) = (0.015, 0.035, 0.12, 1)
        _HorizonColor ("Horizon Color", Color) = (0.13, 0.045, 0.24, 1)
        _BottomColor ("Bottom Color", Color) = (0.005, 0.008, 0.025, 1)
        [HDR] _NebulaA ("Nebula Cyan", Color) = (0.05, 0.95, 1.4, 1)
        [HDR] _NebulaB ("Nebula Magenta", Color) = (1.15, 0.08, 0.85, 1)
        [HDR] _LightningColor ("Lightning Color", Color) = (0.35, 0.9, 2.5, 1)
        [HDR] _StarColor ("Star Color", Color) = (0.9, 0.95, 1.25, 1)
        _Exposure ("Exposure", Range(0, 3)) = 1.35
        _NebulaIntensity ("Nebula Intensity", Range(0, 4)) = 1.65
        _StarIntensity ("Star Intensity", Range(0, 6)) = 2.25
        _LightningIntensity ("Lightning Intensity", Range(0, 5)) = 1.8
        _CoreIntensity ("Core Intensity", Range(0, 8)) = 2.6
        _FlowSpeed ("Flow Speed", Range(0, 3)) = 0.45
        _Rotation ("Rotation", Range(0, 360)) = 20
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

            float4 _TopColor;
            float4 _HorizonColor;
            float4 _BottomColor;
            float4 _NebulaA;
            float4 _NebulaB;
            float4 _LightningColor;
            float4 _StarColor;
            float _Exposure;
            float _NebulaIntensity;
            float _StarIntensity;
            float _LightningIntensity;
            float _CoreIntensity;
            float _FlowSpeed;
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
                float amp = 0.5;
                [unroll]
                for (int i = 0; i < 5; i++)
                {
                    value += noise3(p) * amp;
                    p = p * 2.07 + float3(11.3, 7.1, 3.9);
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

            float starLayer(float3 dir, float scale, float threshold, float twinkle)
            {
                float2 uv = float2(atan2(dir.x, dir.z) * 0.15915494 + 0.5, asin(dir.y) * 0.31830989 + 0.5);
                float2 cell = floor(uv * scale);
                float2 local = frac(uv * scale) - 0.5;
                float seed = hash31(float3(cell, scale));
                float sparkle = smoothstep(threshold, 1.0, seed);
                float core = smoothstep(0.12, 0.0, length(local));
                float pulse = 0.7 + 0.3 * sin(_Time.y * twinkle + seed * 37.0);
                return sparkle * core * pulse;
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
                float time = _Time.y * _FlowSpeed;
                float3 dir = normalize(i.dir);
                dir = rotateY(dir, radians(_Rotation) + time * 0.025);

                float vertical = saturate(dir.y * 0.5 + 0.5);
                float horizon = pow(1.0 - abs(dir.y), 2.6);
                float3 sky = lerp(_BottomColor.rgb, _TopColor.rgb, vertical);
                sky = lerp(sky, _HorizonColor.rgb, horizon * 0.75);

                float3 flowA = dir * 3.0 + float3(time * 0.18, time * 0.07, -time * 0.12);
                float3 flowB = rotateY(dir, 1.15) * 5.2 + float3(-time * 0.05, time * 0.14, time * 0.09);
                float cloud = fbm(flowA);
                float filament = fbm(flowB);
                float nebulaMask = smoothstep(0.42, 0.88, cloud) * smoothstep(0.12, 0.95, 1.0 - abs(dir.y) + filament * 0.35);
                float3 nebula = lerp(_NebulaA.rgb, _NebulaB.rgb, saturate(filament * 1.35));
                sky += nebula * nebulaMask * _NebulaIntensity;

                float angle = atan2(dir.z, dir.x);
                float wave = sin(angle * 5.0 + dir.y * 11.0 + time * 2.2);
                float crackNoise = fbm(dir * 14.0 + float3(time * 1.1, -time * 0.4, time * 0.65));
                float lightning = pow(saturate(1.0 - abs(wave) * 6.5), 3.0) * smoothstep(0.45, 0.86, crackNoise);
                lightning *= saturate(1.0 - abs(dir.y + 0.05) * 0.9);
                sky += _LightningColor.rgb * lightning * _LightningIntensity;

                float3 coreDir = normalize(float3(0.22, 0.08, 1.0));
                float core = pow(saturate(dot(dir, coreDir)), 42.0);
                float halo = pow(saturate(dot(dir, coreDir)), 8.0);
                sky += (_LightningColor.rgb * core * 3.0 + _NebulaB.rgb * halo * 0.45) * _CoreIntensity;

                float stars = starLayer(dir, 170.0, 0.982, 2.4);
                stars += starLayer(rotateY(dir, 0.73), 330.0, 0.992, 3.7) * 0.75;
                stars += starLayer(rotateY(dir, -1.27), 620.0, 0.997, 5.0) * 0.45;
                sky += _StarColor.rgb * stars * _StarIntensity * (0.35 + vertical * 0.65);

                sky *= _Exposure;
                sky = 1.0 - exp(-sky);
                return fixed4(sky, 1.0);
            }
            ENDCG
        }
    }

    FallBack Off
}
