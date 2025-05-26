using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace LLM
{
    namespace LlamaCpp
    {
        // Clase que representa un modelo de lengauje
        public class LlamaModel
        {
            private static bool initializedBackend = false;

            // Puntero al modelo
            public IntPtr Model { get; private set; }
            // Puntero al vocabulario del modelo, que son todos los tokes que dispone para generar texto
            public IntPtr Vocab { get; private set; }
            public ChatTemplate ChatTemplate { get; private set; }

            private LlamaModel(IntPtr modelPointer, ChatTemplate chatTemplate)
            {
                Model = modelPointer;
                // Se obtiene el vocabulario
                Vocab = NativeLlama.llama_model_get_vocab(Model);
                ChatTemplate = chatTemplate;
            }

            ~LlamaModel()
            {
                if (Model != IntPtr.Zero)
                {
                    // Hay que eliminar el modelo, pues en codigo nativo ha reservador memoria
                    NativeLlama.llama_model_free(Model);
                    Model = IntPtr.Zero;
                }

                if (initializedBackend)
                {
                    NativeLlama.llama_backend_free();
                    initializedBackend = false;
                }
            }

            public static async Task<LlamaModel> LoadFromFileAsync(IModelParams modelParams, CancellationToken token, IProgress<float> progressReporter = null)
            {
                if (!initializedBackend)
                {
                    NativeLlama.llama_backend_init();
                    initializedBackend = true;
                }

                // Se obtienen los parametros con los que inicializar el modelo
                NativeLlama.LlamaModelParams llamaModelParams = NativeLlama.llama_model_default_params();
                llamaModelParams.n_gpu_layers = modelParams.NumberGPULayers;

                if (progressReporter != null)
                {
                    // Callback que muestra el progreso de carga de un modelo
                    NativeLlama.LlamaProgressCallback progressCallback = llamaModelParams.progress_callback;
                    llamaModelParams.progress_callback = (progress, user_data) =>
                    {
                        progressReporter?.Report(Math.Clamp(progress, 0, 1));

                        // Si el usuario ha establecido un callback, comprobamos si quiere cancelarlo
                        if (progressCallback != null && !progressCallback(progress, user_data))
                        {
                            return false;
                        }

                        // Se cancela la carga del modelo
                        if (token.IsCancellationRequested)
                        {
                            return false;
                        }

                        return true;
                    };
                }

                string modelPath = modelParams.ModelPath;
                if (!File.Exists(modelParams.ModelPath))
                {
                    throw new Exception($"Model path doesn't exist: {modelPath}.");
                }

                // Se trata de cargar el modelo en un hilo de la pool de hilos
                IntPtr modelPointer = await Task.Run(() =>
                {
                    IntPtr aux = NativeLlama.llama_model_load_from_file(modelPath, llamaModelParams);
                    if (aux == IntPtr.Zero)
                    {
                        throw new Exception("Unable to load the model.");
                    }
                    progressReporter?.Report(1);
                    return aux;
                }, token);

                return new LlamaModel(modelPointer, modelParams.ChatTemplate);
            }

            // Tokenizar un texto, es decir, convertirlo en tokens (trozos de palabras)
            public Int32[] Tokenize(string text, bool addBos = true, bool special = true)
            {
                Int32 nTokens = -NativeLlama.llama_tokenize(Vocab, text, text.Length, null, 0, addBos, special);
                Int32[] tokens = new Int32[nTokens];
                Int32 err = NativeLlama.llama_tokenize(Vocab, text, text.Length, tokens, tokens.Length, addBos, special);
                if (err < 0)
                {
                    throw new Exception("Failed to tokenize the text.");
                }
                return tokens;
            }

            // Si el token producido es el EOS tokens, es que la generacion ha te terminado
            public bool IsEndOfGeneration(Int32 token)
            {
                return NativeLlama.llama_vocab_is_eog(Vocab, token);
            }

            // Convertir el tokenId en texto comprensible por el humano
            public string TokenToPiece(Int32 token)
            {
                byte[] buffer = new byte[128];
                Int32 err = NativeLlama.llama_token_to_piece(Vocab, token, buffer, buffer.Length, 0, true);
                if (err < 0)
                {
                    throw new Exception("Failed to convert token to piece.");
                }
                return Encoding.UTF8.GetString(buffer).TrimEnd('\0');
            }
        }
    }
}