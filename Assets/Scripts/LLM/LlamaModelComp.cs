using System;
using UnityEngine;

namespace LLM
{
    // Clase que se usa para crear un modelo local (cargado en memoria o en un servidor)
    public class LlamaModelComp : MonoBehaviour
    {
        [SerializeField]
        protected string modelPath;
        [SerializeField]
        protected ChatTemplateType fallbackChatTemplate = ChatTemplateType.CHAT_ML;
        [Tooltip("Tamano de la ventana de contexto.")]
        [SerializeField]
        protected int contextSize = 2048;
        [Tooltip("Numero de capas del modelo que procesa la GPU.\n" +
            "0 = no se usa la GPU.")]
        [SerializeField]
        protected int nGPULayers = 999;
        [Tooltip("Tamano del prompt para el proceso del prompt.")]
        [SerializeField]
        protected int batchSize = 512;

        public ChatTemplate ChatTemplate { get; private set; }

        protected virtual void Start()
        {
            // Se lee el modelo
            using (LlamaCpp.GGUFReader ggufReader = new LlamaCpp.GGUFReader(modelPath))
            {
                // Se obtiene el contexto y se limita en caso de superarlo
                int maxContextSize = Convert.ToInt32(ggufReader.GetContextSize());
                if (maxContextSize > 0)
                {
                    contextSize = Math.Min(contextSize, maxContextSize);
                }

                // Se trata de inferir el tipo de plantilla de chat automaticamente
                ChatTemplate = ChatTemplate.GetTemplateFromGGUF(ggufReader);
                if (ChatTemplate == null)
                {
                    ChatTemplate = ChatTemplate.GetTemplate(fallbackChatTemplate);
                }
            };
        }

        public void Load(string modelPath, ChatTemplateType fallbackChatTemplate)
        {
            this.modelPath = modelPath;
            this.fallbackChatTemplate = fallbackChatTemplate;
        }
    }
}
