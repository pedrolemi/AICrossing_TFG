using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

namespace LLM
{
    namespace LlamaCpp
    {
        public class LlamaCppModel : LlamaModelComp
        {
            private CancellationTokenSource cancellationToken;
            private ModelParams modelParams;
            private LlamaModel llamaModel;

            // Se usa este evento para comunicar a quien se suscriba cuando se ha terminado
            // de cargar el modelo, de modo que ya se pueden interactuar con el
            private UnityEvent loadedModel;

            protected void Awake()
            {
                loadedModel = new UnityEvent();
            }

            protected async override void Start()
            {
                base.Start();

                cancellationToken = new CancellationTokenSource();

                modelParams = new ModelParams()
                {
                    ModelPath = modelPath,
                    NumberGPULayers = nGPULayers,
                    ChatTemplate = ChatTemplate,
                    ContextSize = contextSize,
                    BatchSize = batchSize
                };

                string modelName = Path.GetFileName(modelPath);
                Progress<float> progress = new Progress<float>(progress => Debug.Log($"Loading {modelName} progress: {progress}."));
                llamaModel = await LlamaModel.LoadFromFileAsync(modelParams, cancellationToken.Token, progress);
                loadedModel?.Invoke();
            }

            // Interactuar con el modelo manteniendo la memoria
            public LlamaInteractiveInference CreateInteractiveInference()
            {
                if (llamaModel == null)
                {
                    return null;
                }

                LlamaContext llamaContext = new LlamaContext(llamaModel, modelParams);
                return new LlamaInteractiveInference(llamaModel, llamaContext);
            }

            // Interactuar con el modelo sin mantener la memoria
            public LlamaStatelessInference CreateStatelessInference()
            {
                if (llamaModel == null)
                {
                    return null;
                }

                return new LlamaStatelessInference(llamaModel, modelParams);
            }

            // Enviar una peticon al modelo
            public async Task<string> RunAsync(LlamaInference inference, ChatBufferMemory chatMemory, InferenceParams inferenceParams, CancellationToken cancellationToken,
                Action<string> onHandle, Action<string> onComplete, bool stream, Dictionary<string, string> replacements = null)
            {
                string result = "";
                await foreach (string response in inference.RunAsync(chatMemory, inferenceParams, cancellationToken, replacements))
                {
                    result = response.Trim();
                    if (stream && !string.IsNullOrEmpty(result))
                    {
                        onHandle?.Invoke(result);
                    }
                }
                if (!stream)
                {
                    onHandle?.Invoke(result);
                }
                onComplete?.Invoke(result);
                return result;
            }

            public void AddLoadedModelListener(UnityAction action)
            {
                loadedModel.AddListener(action);
            }

            private void OnDestroy()
            {
                cancellationToken.Cancel();
                cancellationToken.Dispose();
            }
        }
    }
}
