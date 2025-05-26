using System;
using System.Runtime.InteropServices;

namespace LLM
{
    namespace LlamaCpp
    {
        public static class NativeGGUF
        {
            private const string DLL_NAME = LlamaCppLibrary.GGML_BASE;

            [StructLayout(LayoutKind.Sequential)]
            public struct GGUFInitParams
            {
                [MarshalAs(UnmanagedType.I1)]
                public bool no_alloc;

                public IntPtr context;
            };

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            public static extern IntPtr gguf_init_from_file(string fname, GGUFInitParams params_init);

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            public static extern void gguf_free(IntPtr context);

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            public static extern Int64 gguf_find_key(IntPtr context, string key);

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi)]
            public static extern IntPtr gguf_get_val_str(IntPtr context, Int64 key_id);

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            public static extern UInt32 gguf_get_val_u32(IntPtr context, Int64 key_id);
        }
    }
}