using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace LLM
{
    namespace LlamaCpp
    {
        // Clase que permite la inferencia con un modelo sin mantener la memoria
        public class LlamaStatelessInference : LlamaInference
        {
            private IContextParams contextParams;

            public LlamaStatelessInference(LlamaModel llamaModel, IContextParams contextParams) : base(llamaModel)
            {
                this.contextParams = contextParams;
            }

            public async IAsyncEnumerable<string> RunAsync(string query, InferenceParams inferenceParams, [EnumeratorCancellation] CancellationToken token = default)
            {
                // Como el contexto esta cacheado, hay que reiniciarlo en cada peticion
                if (llamaContext != null)
                {
                    llamaContext.Dispose();
                    llamaContext = null;
                }

                llamaContext = new LlamaContext(llamaModel, contextParams);

                string result = "";
                await foreach (string chunk in RunInternal(query, inferenceParams, token))
                {
                    result += chunk;
                    yield return result;
                }
            }

            public override async IAsyncEnumerable<string> RunAsync(ChatBufferMemory chatMemory, InferenceParams inferenceParams, [EnumeratorCancellation] CancellationToken token, Dictionary<string, string> replacements = null)
            {
                string query = chatMemory.FormatPrompt(llamaModel.ChatTemplate, true, true, replacements);

                await foreach (string result in RunAsync(query, inferenceParams, token))
                {
                    yield return result;
                }
            }
        }
    }
}