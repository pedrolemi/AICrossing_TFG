using System;
using System.Collections.Generic;

namespace LLM
{
    namespace LlamaCpp
    {
        public class InferenceParams
        {
            public int MaxTokens { get; set; } = -1;
            // El modelo transformer devuelve una probabiliad para un numero de tokens,
            // donde luego hay que elegir que token seleccionar
            public SamplingChain SamplingChain { get; set; } = new DefaultSamplingChain();
            public Dictionary<string, string> Replacements { get; set; }
        }

        // Clase que imlementa una cadena un token de entre los dados
        public abstract class SamplingChain
        {
            private IntPtr samplerChain;

            ~SamplingChain()
            {
                if (samplerChain != IntPtr.Zero)
                {
                    NativeLlama.llama_sampler_free(samplerChain);
                    samplerChain = IntPtr.Zero;
                }
            }

            protected abstract IntPtr CreateSamplerChain(IntPtr context);
            public Int32 Sample(IntPtr context, int index = -1)
            {
                if (samplerChain == IntPtr.Zero)
                {
                    // Se selecciona un token de los generados esta vez
                    samplerChain = CreateSamplerChain(context);
                }
                return NativeLlama.llama_sampler_sample(samplerChain, context, index);
            }
        }

        // Clase que implementa la aproximacion mas sencilla, es decir, seleccionar el token con mayor probabildiad
        public class GreedySamplingChain : SamplingChain
        {
            protected override IntPtr CreateSamplerChain(IntPtr context)
            {
                NativeLlama.LlamaSamplerChainParams samplerParams = NativeLlama.llama_sampler_chain_default_params();
                samplerParams.no_perf = false;
                IntPtr samplerChain = NativeLlama.llama_sampler_chain_init(samplerParams);

                IntPtr greedySampler = NativeLlama.llama_sampler_init_greedy();
                NativeLlama.llama_sampler_chain_add(samplerChain, greedySampler);

                return samplerChain;
            }
        }

        // Clase qeu implementa tecnicas mas avanzadas
        public class DefaultSamplingChain : SamplingChain
        {
            public float Temperature { get; set; } = 0.75f;
            public float MinP { get; set; } = 0.1f;
            public int MinKeep { get; set; } = 1;
            public uint Seed { get; set; } = GetRandomSeed();

            private static uint GetRandomSeed()
            {
                return (uint)UnityEngine.Random.Range(0, int.MaxValue) + (uint)UnityEngine.Random.Range(0, int.MaxValue);
            }

            protected override IntPtr CreateSamplerChain(IntPtr context)
            {
                NativeLlama.LlamaSamplerChainParams samplerParams = NativeLlama.llama_sampler_chain_default_params();
                samplerParams.no_perf = false;
                IntPtr samplerChain = NativeLlama.llama_sampler_chain_init(samplerParams);

                IntPtr minPSampler = NativeLlama.llama_sampler_init_min_p(MinP, MinKeep);
                NativeLlama.llama_sampler_chain_add(samplerChain, minPSampler);

                IntPtr tempSampler = NativeLlama.llama_sampler_init_temp(Temperature);
                NativeLlama.llama_sampler_chain_add(samplerChain, tempSampler);

                IntPtr distSampler = NativeLlama.llama_sampler_init_dist(Seed);
                NativeLlama.llama_sampler_chain_add(samplerChain, distSampler);

                return samplerChain;
            }
        }
    }
}
