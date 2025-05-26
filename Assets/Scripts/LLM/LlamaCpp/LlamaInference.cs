using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace LLM
{
    namespace LlamaCpp
    {
        // Clase que permtie interactuar con un modelo en local
        // Hay que recordar que un modelo de lenguaje solo es capaz de completar texto
        public abstract class LlamaInference
        {
            protected LlamaModel llamaModel;
            protected LlamaContext llamaContext;

            public LlamaInference(LlamaModel llamaModel)
            {
                this.llamaModel = llamaModel;
            }

            protected async IAsyncEnumerable<string> RunInternal(string query, InferenceParams inferenceParams, [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                // Se tokeniza la peticion
                Int32[] tokenIds = llamaModel.Tokenize(query, true, true);

                // En vez de procesar los tokens de uno en uno, se procesan por batchs para paralelizar al modelo
                // y hacer que vaya mas rapido
                NativeLlama.LlamaBatch batch = new NativeLlama.LlamaBatch()
                {
                    token = tokenIds,
                    n_tokens = tokenIds.Length
                };

                int maxTokens = inferenceParams.MaxTokens < 0 ? int.MaxValue : inferenceParams.MaxTokens;

                bool end = false;
                // La generacion termina cuando se encuentra el EOS token o cuando se ha llegado al maxino numero de tokens establecidos
                while (maxTokens > 0 && !end && !cancellationToken.IsCancellationRequested)
                {
                    // Si se excede el contexto, hay un problema
                    int contextSize = llamaContext.ContextSize;
                    int contextSizeUsed = llamaContext.ContextSizeUsed;
                    if (contextSizeUsed + batch.n_tokens > contextSize)
                    {
                        // Existen tecnicas como quedarse con un cierto numero de tokens, pero no se han implementado
                        throw new Exception("Context size exceeded.");
                    }

                    // Ejecutar el decode
                    await llamaContext.DecodeAsync(batch, cancellationToken);

                    // Samplear el siguiente tokens
                    Int32 newTokenId = inferenceParams.SamplingChain.Sample(llamaContext.Context);

                    // Fin de la generacion porque el token es el EOS token
                    if (llamaModel.IsEndOfGeneration(newTokenId))
                    {
                        end = true;
                    }
                    else
                    {
                        // Se convierte el token a texto y se devuelve
                        string chunk = llamaModel.TokenToPiece(newTokenId);
                        yield return chunk;

                        // Se procesa el siguiente token
                        batch.token = new Int32[] { newTokenId };
                        batch.n_tokens = 1;
                        maxTokens -= batch.n_tokens;
                    }
                }
            }

            public abstract IAsyncEnumerable<string> RunAsync(ChatBufferMemory chatHistory, InferenceParams inferenceParams, CancellationToken token, Dictionary<string, string> replacements = null);
        }
    }
}