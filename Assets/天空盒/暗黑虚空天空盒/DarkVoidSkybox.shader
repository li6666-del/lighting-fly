Shader "Skybox/Dark Void Storm Procedural"
{
    Properties
    {
        _ZenithColor ("Zenith Color", Color) = (0.002, 0.0025, 0.008, 1)
        _HorizonColor ("Horizon Color", Color) = (0.03, 0.006, 0.035, 1)
        _AbyssColor ("Abyss Color", Color) = (0.0005, 0.0005, 0.003, 1)
        [HDR] _RiftColor ("Rift Blood Glow", Color) = (1.6, 0.08, 0.045, 1)
        [HDR] _VoidViolet ("Void Violet", Color) = (0.32, 0.02, 0.72, 1)
        [HDR] _ColdEmbers ("Cold Embers", Color) = (0.25, 0.75, 1.15, 1)
        _Exposure ("Exposure", Range(0, 3)) = 1.1
        _Darkness ("Darkness", Range(0, 1)) = 0.28
        _StormIntensity ("Storm Intensity", Range(0, 5)) = 2.1
        _RiftIntensity ("Rift Intensity", Range(0, 6)) = 2.8
        _EmberIntensity ("Ember Intensity", Range(0, 6)) = 1.15
        _VortexSpeed ("Vortex Speed", Range(0, 3)) = 0.38
        _RiftSharpness ("Rift Sharpness", Range(1, 18)) = 9.0
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
            float4 _AbyssColor;
            float4 _RiftColor;
            float4 _VoidViolet;
            float4 _ColdEmbers;
            float _Exposure;
            float _Darkness;
            float _StormIntensity;
            float _RiftIntensity;
            float _EmberIntensity;
            float _VortexSpeed;
            float _RiftSharpness;
            float _Rotation;

            float hash31(float3 p)
            {
                p = frac(p * 0.1031);
                p += dot(p, p.yzx + 31.32);
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
                    p = p * 2.04 + float3(9.7, 4.6, 12.1);
                    amp *= 0.52;
                }
                return value;
            }

            float3 rotateY(float3 p, float radians)
            {
                float s = sin(radians);
                float c = cos(radians);
                return float3(c * p.x - s * p.z, p.y, s * p.x + c * p.z);
            }

            float emberLayer(float3 dir, float scale, float threshold, float speed)
            {
                float2 uv = float2(atan2(dir.x, dir.z) * 0.15915494 + 0.5, asin(dir.y) * 0.31830989 + 0.5);
                float2 cell = floor(uv * scale);
                float2 local = frac(uv * scale) - 0.5;
                float seed = hash31(float3(cell, scale));
                float active = smoothstep(threshold, 1.0, seed);
                float core = smoothstep(0.11, 0.0, length(local));
                float flicker = 0.55 + 0.45 * sin(_Time.y * speed + seed * 55.0);
                return active * core * flicker;
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
                float time = _Time.y * _VortexSpeed;
                float3 dir = normalize(i.dir);
                dir = rotateY(dir, radians(_Rotation) - time * 0.035);

                float vertical = saturate(dir.y * 0.5 + 0.5);
                float horizon = pow(1.0 - abs(dir.y), 3.2);
                float3 sky = lerp(_AbyssColor.rgb, _ZenithColor.rgb, vertical);
                sky = lerp(sky, _HorizonColor.rgb, horizon * 0.8);

                float angle = atan2(dir.z, dir.x);
                float radial = sqrt(max(0.001, dir.x * dir.x + dir.z * dir.z));
                float spiral = angle * 2.4 + radial * 7.0 - time * 1.4 + dir.y * 2.0;

                float3 swirlP = float3(dir.x * 4.2 + sin(spiral) * 0.8, dir.y * 3.0, dir.z * 4.2 + cos(spiral) * 0.8);
                float smokeA = fbm(swirlP + float3(time * 0.28, -time * 0.12, time * 0.18));
                float smokeB = fbm(rotateY(dir, 1.4) * 7.5 + float3(-time * 0.35, time * 0.2, time * 0.08));
                float storm = smoothstep(0.38, 0.9, smokeA) * smoothstep(0.16, 0.95, smokeB);
                storm *= saturate(1.1 - abs(dir.y) * 0.75);
                sky += lerp(_VoidViolet.rgb, _RiftColor.rgb * 0.35, smokeB) * storm * _StormIntensity;

                float crack = sin(spiral * 2.7 + smokeB * 5.5);
                float rift = pow(saturate(1.0 - abs(crack) * _RiftSharpness), 2.25);
                rift *= smoothstep(0.45, 0.82, smokeA + smokeB * 0.25);
                rift *= saturate(1.0 - abs(dir.y + 0.02) * 0.95);
                float pulse = 0.75 + 0.25 * sin(_Time.y * 2.0 + smokeA * 9.0);
                sky += _RiftColor.rgb * rift * _RiftIntensity * pulse;

                float3 mawDir = normalize(float3(-0.28, -0.06, 1.0));
                float maw = pow(saturate(dot(dir, mawDir)), 24.0);
                float mawHalo = pow(saturate(dot(dir, mawDir)), 6.0);
                float occlusion = 1.0 - maw * (0.65 + _Darkness * 0.35);
                sky *= occlusion;
                sky += _VoidViolet.rgb * mawHalo * storm * 0.55;

                float embers = emberLayer(dir, 150.0, 0.988, 2.2);
                embers += emberLayer(rotateY(dir, 0.91), 310.0, 0.995, 4.4) * 0.55;
                embers += emberLayer(rotateY(dir, -1.8), 520.0, 0.998, 6.0) * 0.35;
                float emberTint = step(0.54, hash31(floor(dir * 40.0)));
                sky += lerp(_RiftColor.rgb, _ColdEmbers.rgb, emberTint) * embers * _EmberIntensity;

                sky *= lerp(1.0, 0.34, _Darkness);
                sky *= _Exposure;
                sky = 1.0 - exp(-sky);
                return fixed4(sky, 1.0);
            }
            ENDCG
        }
    }

    FallBack Off
}
