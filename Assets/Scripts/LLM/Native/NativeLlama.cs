using System;
using System.Runtime.InteropServices;

namespace LLM
{
    namespace LlamaCpp
    {
        // Se usa P/Invoke, para llamar funcion de codigo nativo (C y C++)
        // Se usa Marshal, una clase de .NET para realizar la conversiones de tipo necesarios entre los lenguajes

        // LlamaCpp es una liberria escrita en C y C++, que permite la ejecucion de un modelo de lenguaje .gguf
        public static class NativeLlama
        {
            private const string DLL_NAME = LlamaCppLibrary.LLAMA;

            public enum LLamaSplitMode
            {
                LLAMA_SPLIT_MODE_NONE = 0,
                LLAMA_SPLIT_MODE_LAYER = 1,
                LLAMA_SPLIT_MODE_ROW = 2,
            };

            // Se trata de un callback, que se llama en codigo nativo
            [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
            public delegate bool LlamaProgressCallback(float progress, IntPtr user_data);

            // El struct tiene que ser Sequential y tener el mismo orden que en C, para asegurar que asignan bien todos los datos
            [StructLayout(LayoutKind.Sequential)]
            public struct LlamaModelParams
            {
                // Para gestionar memoria nativa hay dos opciones:
                // - Usar la palabra clave unsafe, que permite interactuar con punteros
                // - Usar IntPtr, que es un entero con el mismo ancho de bits que un puntero,
                // por lo tanto, puede guardar direcciones de memoria (varia en funcion de la plataforma)
                public IntPtr devices;

                public Int32 n_gpu_layers;
                public LLamaSplitMode split_mode;

                public Int32 main_gpu;

                public IntPtr tensor_split;

                public LlamaProgressCallback progress_callback;

                public IntPtr progress_callback_user_data;

                public IntPtr kv_overrides;

                // Indica que se trata de 0 o 1 en codigo nativo, pues en C no existen los booleanos
                [MarshalAs(UnmanagedType.I1)]
                public bool vocab_only;
                [MarshalAs(UnmanagedType.I1)]
                public bool use_mmap;
                [MarshalAs(UnmanagedType.I1)]
                public bool use_mlock;
                [MarshalAs(UnmanagedType.I1)]
                public bool check_tensors;
            }

            public enum LlamaRopeScalingType
            {
                LLAMA_ROPE_SCALING_TYPE_UNSPECIFIED = -1,
                LLAMA_ROPE_SCALING_TYPE_NONE = 0,
                LLAMA_ROPE_SCALING_TYPE_LINEAR = 1,
                LLAMA_ROPE_SCALING_TYPE_YARN = 2,
                LLAMA_ROPE_SCALING_TYPE_LONGROPE = 3,
                LLAMA_ROPE_SCALING_TYPE_MAX_VALUE = LLAMA_ROPE_SCALING_TYPE_LONGROPE,
            };

            public enum LlamaPoolingType
            {
                LLAMA_POOLING_TYPE_UNSPECIFIED = -1,
                LLAMA_POOLING_TYPE_NONE = 0,
                LLAMA_POOLING_TYPE_MEAN = 1,
                LLAMA_POOLING_TYPE_CLS = 2,
                LLAMA_POOLING_TYPE_LAST = 3,
                LLAMA_POOLING_TYPE_RANK = 4,
            };

            public enum LlamaAttentionType
            {
                LLAMA_ATTENTION_TYPE_UNSPECIFIED = -1,
                LLAMA_ATTENTION_TYPE_CAUSAL = 0,
                LLAMA_ATTENTION_TYPE_NON_CAUSAL = 1,
            };

            // NOTE: always add types at the end of the enum to keep backward compatibility
            public enum GGMLType
            {
                GGML_TYPE_F32 = 0,
                GGML_TYPE_F16 = 1,
                GGML_TYPE_Q4_0 = 2,
                GGML_TYPE_Q4_1 = 3,
                // GGML_TYPE_Q4_2 = 4, support has been removed
                // GGML_TYPE_Q4_3 = 5, support has been removed
                GGML_TYPE_Q5_0 = 6,
                GGML_TYPE_Q5_1 = 7,
                GGML_TYPE_Q8_0 = 8,
                GGML_TYPE_Q8_1 = 9,
                GGML_TYPE_Q2_K = 10,
                GGML_TYPE_Q3_K = 11,
                GGML_TYPE_Q4_K = 12,
                GGML_TYPE_Q5_K = 13,
                GGML_TYPE_Q6_K = 14,
                GGML_TYPE_Q8_K = 15,
                GGML_TYPE_IQ2_XXS = 16,
                GGML_TYPE_IQ2_XS = 17,
                GGML_TYPE_IQ3_XXS = 18,
                GGML_TYPE_IQ1_S = 19,
                GGML_TYPE_IQ4_NL = 20,
                GGML_TYPE_IQ3_S = 21,
                GGML_TYPE_IQ2_S = 22,
                GGML_TYPE_IQ4_XS = 23,
                GGML_TYPE_I8 = 24,
                GGML_TYPE_I16 = 25,
                GGML_TYPE_I32 = 26,
                GGML_TYPE_I64 = 27,
                GGML_TYPE_F64 = 28,
                GGML_TYPE_IQ1_M = 29,
                GGML_TYPE_BF16 = 30,
                // GGML_TYPE_Q4_0_4_4 = 31, support has been removed from gguf files
                // GGML_TYPE_Q4_0_4_8 = 32,
                // GGML_TYPE_Q4_0_8_8 = 33,
                GGML_TYPE_TQ1_0 = 34,
                GGML_TYPE_TQ2_0 = 35,
                // GGML_TYPE_IQ4_NL_4_4 = 36,
                // GGML_TYPE_IQ4_NL_4_8 = 37,
                // GGML_TYPE_IQ4_NL_8_8 = 38,
                GGML_TYPE_COUNT = 39,
            };

            [StructLayout(LayoutKind.Sequential)]
            public struct LLamaContextParams
            {
                public UInt32 n_ctx;
                public UInt32 n_batch;
                public UInt32 n_ubatch;
                public UInt32 n_seq_max;
                public Int32 n_threads;
                public Int32 n_threads_batch;

                public LlamaRopeScalingType rope_scaling_type;
                public LlamaPoolingType pooling_type;
                public LlamaAttentionType attention_type;

                public float rope_freq_base;
                // Unity crash factor
                public float rope_freq_scale;
                public float yarn_ext_factor;
                public float yarn_attn_factor;
                public float yarn_beta_fast;

                public float yarn_beta_slow;
                public UInt32 yarn_orig_ctx;
                public float defrag_thold;

                public IntPtr cb_eval;
                public IntPtr cb_eval_user_data;

                public GGMLType type_k;
                public GGMLType type_v;

                [MarshalAs(UnmanagedType.I1)]
                public bool logits_all;
                [MarshalAs(UnmanagedType.I1)]
                public bool embeddings;
                [MarshalAs(UnmanagedType.I1)]
                public bool offload_kqv;
                [MarshalAs(UnmanagedType.I1)]
                public bool flash_attn;
                [MarshalAs(UnmanagedType.I1)]
                public bool no_perf;

                public IntPtr abort_callback;
                public IntPtr aobrt_callback_data;
            }

            public struct LlamaSamplerChainParams
            {
                [MarshalAs(UnmanagedType.I1)]
                public bool no_perf;
            }

            [StructLayout(LayoutKind.Sequential)]
            public struct LlamaBatch
            {
                public Int32 n_tokens;
                public Int32[] token;
                public IntPtr embd;
                public IntPtr pos;
                public IntPtr n_seq_id;
                public IntPtr seq_id;
                public IntPtr logits;
            }

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            public static extern void llama_backend_init();
            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            public static extern void llama_backend_free();
            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            public static extern LlamaModelParams llama_model_default_params();

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr llama_model_load_from_file(string model_path, LlamaModelParams model_params);

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr llama_model_get_vocab(IntPtr model);

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            public static extern Int32 llama_tokenize(IntPtr vocab, string text, Int32 text_len, [MarshalAs(UnmanagedType.LPArray)] Int32[] tokens, Int32 n_tokens_max, bool add_special, bool parse_special);

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            public static extern LLamaContextParams llama_context_default_params();

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr llama_init_from_model(IntPtr model, LLamaContextParams context_params);

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            public static extern LlamaSamplerChainParams llama_sampler_chain_default_params();

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr llama_sampler_chain_init(LlamaSamplerChainParams params_sampler);

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            public static extern void llama_sampler_chain_add(IntPtr chain, IntPtr sampler_chain);

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr llama_sampler_init_greedy();

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr llama_sampler_init_min_p(float p, nint min_keep);

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr llama_sampler_init_temp(float temp);

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr llama_sampler_init_dist(UInt32 seed);

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            public static extern Int32 llama_token_to_piece(IntPtr vocab, Int32 token, [MarshalAs(UnmanagedType.LPArray)] byte[] buffer, Int32 length, Int32 lstring, bool special);

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            public static extern Int32 llama_decode(IntPtr context, LlamaBatch batch);

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            public static extern Int32 llama_sampler_sample(IntPtr sampler_chain, IntPtr context, Int32 idx);

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            public static extern bool llama_vocab_is_eog(IntPtr vocab, Int32 token);

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            public static extern void llama_sampler_free(IntPtr sampler_chain);

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            public static extern void llama_free(IntPtr context);

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            public static extern void llama_model_free(IntPtr model);

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            public static extern UInt32 llama_n_ctx(IntPtr context);

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            public static extern Int32 llama_get_kv_cache_used_cells(IntPtr context);
        }
    }
}
