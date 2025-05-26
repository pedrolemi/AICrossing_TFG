using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;

namespace LLM
{
    namespace LlamaCpp
    {
        // Clase que permite la inferencia con un modelo manteniendo la memoria
        public class LlamaInteractiveInference : LlamaInference
        {
            private int messagesPrevLen;

            public LlamaInteractiveInference(LlamaModel llamaModel, LlamaContext llamaContext) : base(llamaModel)
            {
                this.llamaContext = llamaContext;
            }

            public override async IAsyncEnumerable<string> RunAsync(ChatBufferMemory chatHistory, InferenceParams inferenceParams, [EnumeratorCancellation] CancellationToken token, Dictionary<string, string> replacements = null)
            {
                // Se convierten los mensajes en texto plano
                ChatTemplate chatTemplate = llamaModel.ChatTemplate;
                string newMessages = chatHistory.FormatPrompt(chatTemplate, true, true, replacements);

                // Se procesa solo la nueva parte del mensaje porque el resto esta cacheado
                string query = newMessages.Substring(messagesPrevLen);

                string result = "";
                await foreach (string chunk in RunInternal(query, inferenceParams, token))
                {
                    result += chunk;
                    yield return result;
                }

                // Se agrega el texto resultante
                chatHistory.AddAssistantMessage(result);
                string prevMessages = chatHistory.FormatPrompt(chatTemplate, false, true);
                messagesPrevLen = prevMessages.Length;
            }
        }
    }
}