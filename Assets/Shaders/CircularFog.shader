Shader "Custom/CircularFogUltimate"
{
    Properties
    {
        [Header(Colors)]
        [MainColor] _BaseColor("Top Fog Color", Color) = (0.6, 0.7, 0.7, 0.3) // Üst Renk (Daha şeffaf yapıldı)
        _SolidColor("Bottom Solid Color", Color) = (0.0, 0.1, 0.0, 1.0)       // Alt Renk (Zehir yeşili)
        
        [Header(Density Control)]
        _SolidHeight("Solid Bottom Height", Range(0.0, 1.0)) = 0.4      // Alt tarafın ne kadar yukarı çıkacağı
        _BottomStrength("Bottom Hardness", Range(1.0, 5.0)) = 3.0       // Alt tarafın sertliği
        _TopOpacity("Top Opacity Multiplier", Range(0.0, 1.0)) = 0.3    // Üst tarafın görünürlüğünü kısma ayarı
        _Density("General Density", Range(1.0, 10.0)) = 4.0             // Genel yoğunluk

        [Header(Fake Depth)]
        _ParallaxStrength("Fake Depth Strength", Range(0.0, 0.5)) = 0.1 // KAĞIT HİSSİNİ YOK EDEN AYAR
        
        [Header(Noise Settings)]
        _NoiseScale("Noise Scale", Float) = 15.0
        _NoiseSpeed("Noise Speed", Vector) = (0.1, 0.15, 0, 0)
        
        [Header(Shape)]
        _Radius("Radius", Range(0, 0.5)) = 0.48
        _Softness("Edge Softness", Range(0.01, 0.5)) = 0.15
        _DepthFade("Depth Fade Distance", Float) = 1.0

        _WaveSpeed("Wave Speed", Float) = 2.0        // Dalganın hızı
        _WaveAmplitude("Wave Amplitude", Float) = 0.05 // Dalganın yüksekliği (genliği)
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Transparent" 
            
            // ESKİSİ: "Queue" = "Transparent"
            
            // YENİSİ: Şeffaf sırasından 100 adım önce çizilsin.
            // Böylece tüm patlamalar, efektler ve UI bu sisin ÜZERİNE çizilir.
            "Queue" = "Transparent-100"

            "RenderPipeline" = "UniversalPipeline" 
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 screenPos : TEXCOORD1;
                float3 viewDirOS : TEXCOORD3; // Obje uzayında bakış açısı (Derinlik için)
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _SolidColor;
                float _Density;
                float _SolidHeight;
                float _BottomStrength;
                float _TopOpacity;
                float _ParallaxStrength; // YENİ
                float _NoiseScale;
                float4 _NoiseSpeed;
                float _Radius;
                float _Softness;
                float _DepthFade;
                float _WaveSpeed;
                float _WaveAmplitude;
            CBUFFER_END

            // --- NOISE ---
            float random(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453123);
            }

            float noise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                float a = random(i);
                float b = random(i + float2(1.0, 0.0));
                float c = random(i + float2(0.0, 1.0));
                float d = random(i + float2(1.0, 1.0));
                float2 u = f * f * (3.0 - 2.0 * f);
                return lerp(a, b, u.x) + (c - a)* u.y * (1.0 - u.x) + (d - b) * u.x * u.y;
            }
            // -------------

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.screenPos = ComputeScreenPos(output.positionCS);
                
                // Yalancı derinlik için bakış açısını hesapla
                // Kameranın objeye göre nerede olduğu
                float3 viewDirWS = GetWorldSpaceViewDir(TransformObjectToWorld(input.positionOS.xyz));
                output.viewDirOS = TransformWorldToObjectDir(viewDirWS);
                
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                // 1. DAİRESEL MASKE
                float2 centerUV = input.uv - 0.5;
                float dist = length(centerUV);
                float circleMask = 1.0 - smoothstep(_Radius, _Radius + _Softness, dist);

                // --- 2. FAKE DEPTH (PARALLAX) ---
                // "Kağıt gibi" hissini yok eden kısım burası.
                // Bakış açısına göre UV'leri hafifçe kaydırıyoruz.
                // Alt kısımlar sabit kalsın, üst kısımlar daha çok kaysın diye uv.y ile çarptım.
                float2 parallaxOffset = input.viewDirOS.xy * _ParallaxStrength * input.uv.y;
                float2 animatedUV = input.uv + parallaxOffset;
                
                // 3. NOISE
                float2 noiseUV = animatedUV * _NoiseScale + _Time.y * _NoiseSpeed.xy;
                float fogNoise = noise(noiseUV);

                // --- GÜNCELLENMİŞ 4. ADIM: SINE WAVE İLE HAREKETLİ UFUK ÇİZGİSİ ---

                // 1. Yatay dalga hesapla: UV.x ve zamanı kullanarak bir salınım oluşturur
                // _NoiseScale'i dalga sıklığı, _Time.y'yi ise dalga hızı olarak kullanıyoruz
                // _WaveSpeed ile hızı, _WaveAmplitude ile dalgalanma şiddetini çarpan olarak kullanıyoruz
                float wave = sin(input.uv.x * _NoiseScale * 0.5 + _Time.y * _WaveSpeed) * _WaveAmplitude;

                // Bu dalgayı dikey gradyana ekle
                float verticalGradient = 1.0 - smoothstep(0.0, 1.0, input.uv.y + wave);

                // 3. Alt bölge (Solid) sertliğini hesapla
                float bottomZone = smoothstep(1.0 - _SolidHeight, 1.0, verticalGradient);
                bottomZone = pow(bottomZone, 1.0 / _BottomStrength); 

                // 4. Üst bölge (Noise) ve birleştirme
                float topZoneNoise = saturate(fogNoise * _Density) * _TopOpacity;
                float finalAlphaPattern = lerp(topZoneNoise, 1.0, bottomZone);

                // 5. RENK KARIŞIMI
                // Alt taraf (bottomZone yüksek) -> SolidColor
                // Üst taraf -> BaseColor
                half4 finalColor = lerp(_BaseColor, _SolidColor, bottomZone);

                // 6. DEPTH FADE
                float2 screenUV = input.screenPos.xy / input.screenPos.w;
                float rawDepth = SampleSceneDepth(screenUV);
                float sceneLinearDepth = LinearEyeDepth(rawDepth, _ZBufferParams);
                float surfaceLinearDepth = LinearEyeDepth(input.screenPos.z / input.screenPos.w, _ZBufferParams);
                float depthFade = saturate((sceneLinearDepth - surfaceLinearDepth) / _DepthFade);

                // 7. SONUÇ
                finalColor.a *= circleMask * finalAlphaPattern * depthFade;

                if(finalColor.a <= 0.01) discard;

                return finalColor;
            }
            ENDHLSL
        }
    }
}