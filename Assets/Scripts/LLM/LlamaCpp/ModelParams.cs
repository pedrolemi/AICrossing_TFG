namespace LLM
{
    namespace LlamaCpp
    {
        public interface IModelParams
        {
            public string ModelPath { get; set; }
            public int NumberGPULayers { get; set; }
            public ChatTemplate ChatTemplate { get; set; }
        }

        public interface IContextParams
        {
            public int ContextSize { get; set; }
            public int BatchSize { get; set; }
        }

        public class ModelParams : IModelParams, IContextParams
        {
            public string ModelPath { get; set; }
            public int NumberGPULayers { get; set; } = 999;
            public ChatTemplate ChatTemplate { get; set; } = ChatTemplate.GetTemplate(ChatTemplateType.CHAT_ML);

            public int ContextSize { get; set; } = 2048;
            public int BatchSize { get; set; } = 512;
        }
    }
}