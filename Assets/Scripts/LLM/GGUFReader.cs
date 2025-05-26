using System;
using System.Runtime.InteropServices;
using System.IO;

namespace LLM
{
    namespace LlamaCpp
    {
        // Clase que usa la libreria LlamaCpp para leer el modelo de lenguaje y extrar informacion a partir de el
        public class GGUFReader : IDisposable
        {
            private IntPtr context;

            public string ModelPath { get; private set; }

            public GGUFReader(string modelPath)
            {
                ModelPath = modelPath;

                NativeGGUF.GGUFInitParams paramsInit = new NativeGGUF.GGUFInitParams()
                {
                    no_alloc = true,
                    context = IntPtr.Zero
                };

                if (!File.Exists(modelPath))
                {
                    throw new Exception($"Model path doesn't exist: {modelPath}.");
                }
                context = NativeGGUF.gguf_init_from_file(modelPath, paramsInit);
            }

            ~GGUFReader()
            {
                Dispose(false);
            }

            public void Dispose()
            {
                Dispose(true);
                GC.SuppressFinalize(this);
            }

            private void Dispose(bool disposing)
            {
                // unmanaged resources
                NativeGGUF.gguf_free(context);
                context = IntPtr.Zero;
                //if (disposing)
                //{
                //    // dispose managed resources
                //}
            }

            // Se obtiene el contexto del modelo, es decir, la cantidad de tokens que es capaz de procesar en total
            public UInt32 GetContextSize()
            {
                Int64 architectureKeyId = NativeGGUF.gguf_find_key(context, "general.architecture");
                if (architectureKeyId == -1)
                {
                    return 0;
                }
                IntPtr pointer = NativeGGUF.gguf_get_val_str(context, architectureKeyId);

                // Se usa el marshalizador para convertir memoria no gestionada por el sistema en una cadena
                // El comportamiento habitual seria liberar esa memoria posteriormetne, puesto que se entiende que la libreria nativa no lo hace
                // Sin embargo, en este caso si lo hace
                string architecture = Marshal.PtrToStringAnsi(pointer);
                string contextLengthKey = architecture + ".context_length";
                Int64 contextLengthKeyId = NativeGGUF.gguf_find_key(context, contextLengthKey);
                if (contextLengthKeyId == -1)
                {
                    return 0;
                }
                UInt32 contextLength = NativeGGUF.gguf_get_val_u32(context, contextLengthKeyId);
                return contextLength;
            }

            private string GetStringField(string key)
            {
                Int64 keyId = NativeGGUF.gguf_find_key(context, key);
                if (keyId == -1)
                {
                    return "";
                }
                else
                {
                    IntPtr pointer = NativeGGUF.gguf_get_val_str(context, keyId);
                    return Marshal.PtrToStringAnsi(pointer);
                }
            }

            public string GetTemplateChat()
            {
                return GetStringField("tokenizer.chat_template");
            }

            public string GetModelName()
            {
                return GetStringField("general.name");
            }
        }
    }
}