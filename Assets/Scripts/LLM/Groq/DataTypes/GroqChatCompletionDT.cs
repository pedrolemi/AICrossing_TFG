using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Utilities;

namespace LLM
{
    namespace Groq
    {
        #region COMMON

        [Serializable]
        public class Message : ChatMessage
        {
            public List<ToolCall> ToolCalls { get; set; }
            public string ToolCallId { get; set; }
            public string Name { get; set; }
        }
        #endregion

        #region TOOLS
        public enum ToolChoiceType
        {
            [Description("none")]
            NONE,
            [Description("auto")]
            AUTO,
            [Description("required")]
            REQUIRED
        }

        [Serializable]
        public class ToolFunction
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public JObject Parameters { get; set; }
            [JsonIgnore]
            public Func<JObject, string> Execute { get; set; }
        }

        public static class ToolType
        {
            public const string FUNCTION = "function";
        }

        [Serializable]
        public class Tool
        {
            public string Type { get; set; } = ToolType.FUNCTION;
            public ToolFunction Function { get; set; }
        }

        [Serializable]
        public class ToolCallFunction
        {
            public string Name { get; set; }
            public string Arguments { get; set; }
        }

        [Serializable]
        public class ToolCall
        {
            public int Index { get; set; }
            public string Id { get; set; }
            public string Type { get; set; }
            public ToolCallFunction Function { get; set; }
        }
        #endregion

        #region REQUEST
        public enum ResponseType
        {
            [Description("text")]
            TEXT,
            [Description("json_object")]
            JSON_OBJECT
        }

        [Serializable]
        public class ResponseFormat
        {
            public string Type { get; set; } = ResponseType.TEXT.GetDescriptionCached();
        }

        public enum Model
        {
            [Description("gemma2-9b-it")]
            GEMMA2_9B_IT,
            [Description("llama-3.3-70b-versatile")]
            LLAMA_3_3_70B_VERSATILE,
            [Description("llama-3.1-8b-instant")]
            LLAMA_3_1_8b_INSTANT,
            [Description("llama3-70b-8192")]
            LLAMA3_70B_8192,
            [Description("llama3-8b-8192")]
            LLAMA3_8B_8192,
            //[Description("mixtral-8x7b-32768")]
            //MIXTRAL_8X7B_32768
            [Description("mistral-saba-24b")]
            MISTRAL_SABA_24B
        }

        //public enum ReasoningType
        //{
        //    NONE,
        //    [Description("raw")]
        //    RAW,
        //    [Description("parsed")]
        //    PARSED,
        //    [Description("hidden")]
        //    HIDDEN
        //}

        [Serializable]
        public class ChatCompletionRequest : ICompletionRequest
        {
            public float? FrequencyPenalty { get; set; } = 0.0f;
            public int? MaxCompletionTokens { get; set; }
            public bool ParallelToolCalls { get; set; } = true;
            public string Model { get; set; }
            public List<Message> Messages { get; set; }
            public float PresencePenalty { get; set; } = 0.0f;
            //public string ReasoningFormat { get; set; }
            public ResponseFormat ResponseFormat { get; set; }
            public Int64? Seed { get; set; }
            public List<string> Stop { get; set; }
            public bool Stream { get; set; } = false;
            public float? Temperature { get; set; } = 1.0f;
            public string ToolChoice { get; set; }
            public List<Tool> Tools { get; set; }
            public float? TopP { get; set; } = 1.0f;
        }
        #endregion

        #region RESPONSE
        [Serializable]
        public struct Choice
        {
            public int Index { get; set; }
            public Message Message { get; set; }
            public Message Delta { get; set; }
            public string FinishReason { get; set; }
            public string Logprobs { get; set; }
        }

        [Serializable]
        public struct Usage
        {
            public float QueueTime { get; set; }
            public int PromptTokens { get; set; }
            public float PromptTime { get; set; }
            public int CompletionTokens { get; set; }
            public float CompletionTime { get; set; }
            public int TotalTokens { get; set; }
            public float TotalTime { get; set; }
        }

        [Serializable]
        public struct XGroq
        {
            public string Id { get; set; }
            public Usage Usage { get; set; }
        }

        [Serializable]
        public class ChatCompletionResponse
        {
            public string Id { get; set; }
            public string Object { get; set; }
            public int Created { get; set; }
            public string Model { get; set; }
            public List<Choice> Choices { get; set; }
            public Usage Usage { get; set; }
            public string SystemFingerprint { get; set; }
            public XGroq XGroq { get; set; }
        }
        #endregion
    }
}
