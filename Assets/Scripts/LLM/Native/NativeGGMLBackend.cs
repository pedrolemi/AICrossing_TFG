using System.Runtime.InteropServices;

namespace LLM
{
    namespace LlamaCpp
    {
        public static class NativeGGMLBackend
        {
            private const string DLL_NAME = LlamaCppLibrary.GGML;

            [DllImport(DLL_NAME, CallingConvention = CallingConvention.Cdecl)]
            public static extern void ggml_backend_load_all();
        }
    }
}
