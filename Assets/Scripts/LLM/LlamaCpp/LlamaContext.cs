using System;
using System.Threading;
using System.Threading.Tasks;

namespace LLM
{
    namespace LlamaCpp
    {
        // Clase que representa el contexto del modelo, es decir, la capacidad de tokens que es capaz de procesar
        // Ademas en LlamaCpp el contexto es cacheado mediante KV caching
        // para que no haga falta procesar todos los tokens anteriores en cada peticion
        public class LlamaContext : IDisposable
        {
            // Puntero al contexto
            // Puede haber multiples contxto para un solo modelo
            public IntPtr Context { get; private set; }

            public Int32 ContextSize => Convert.ToInt32(NativeLlama.llama_n_ctx(Context));
            public Int32 ContextSizeUsed => NativeLlama.llama_get_kv_cache_used_cells(Context);

            public LlamaContext(LlamaModel llamaModel, IContextParams contextParams)
            {
                NativeLlama.LLamaContextParams llamaContextParams = NativeLlama.llama_context_default_params();
                llamaContextParams.n_ctx = Convert.ToUInt32(contextParams.ContextSize);
                llamaContextParams.n_batch = Convert.ToUInt32(contextParams.BatchSize);
                llamaContextParams.no_perf = false;

                Context = NativeLlama.llama_init_from_model(llamaModel.Model, llamaContextParams);
            }

            ~LlamaContext()
            {
                Dispose(false);
            }

            // Se implementa el patron Dispose para poder el contexto cuando sea necesario
            // Como el contexot esta cacheado, siempre tiene constancia de la informacion anterior
            // Por lo tanto, si se quiere enviar una peticion de una sola vez hay que borrar el contexto y volver a crearlo
            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            private void Dispose(bool disposing)
            {
                // unmanaged resources
                if (Context != IntPtr.Zero)
                {
                    NativeLlama.llama_free(Context);
                    Context = IntPtr.Zero;
                }
                //if (disposing)
                //{
                //    // dispose managed resources
                //}
            }

            // Ejecutar el Decode del modelo Transformer en un hilo de la pool de hilos
            public async Task DecodeAsync(NativeLlama.LlamaBatch batch, CancellationToken token)
            {
                await Task.Run(() =>
                {
                    Int32 err = NativeLlama.llama_decode(Context, batch);
                    if (err != 0)
                    {
                        throw new Exception("Failed to evaluate.");
                    }
                }, token);
            }
        }
    }
}