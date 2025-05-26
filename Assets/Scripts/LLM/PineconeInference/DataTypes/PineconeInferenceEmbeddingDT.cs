using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace PineconeInference
{
    #region REQUEST
    public static class InputTypeClass
    {
        public const string QUERY = "query";
        public const string PASSAGE = "passage";
    }

    public static class TruncateType
    {
        public const string END = "END";
        public const string NONE = "NONE";
    }

    [Serializable]
    public class EmbeddingParameters
    {
        public string InputType { get; set; }
        public string Truncate { get; set; } = TruncateType.END;
    }

    [Serializable]
    public class TextContainer
    {
        public string Text { get; set; }
    }

    public enum EmbeddingModel
    {
        [Description("llama-text-embed-v2")]
        LLAMA_TEXT_EMBED_V2,
        [Description("multilingual-e5-large")]
        MULTILINGUAL_E5_LARGE,
    }

    [Serializable]
    public class EmbeddingRequest
    {
        public string Model { get; set; }
        public EmbeddingParameters Parameters { get; set; }
        public List<TextContainer> Inputs { get; set; }
    }
    #endregion

    #region RESPONSE
    public static class VectorType
    {
        public const string DENSE = "dense";
        public const string SPARSE = "sparse";
    }

    public interface IDenseEmbeddingData
    {
        public float[] Values { get; set; }
    }

    public interface ISparseEmbeddingData
    {
        public float[] SparseValues { get; set; }
        public int[] SparseIndices { get; set; }
        public string[] SparseTokens { get; set; }

    }

    [Serializable]
    public struct EmbeddingData
    {
        public float[] Values { get; set; }
        public float[] SparseValues { get; set; }
        public int[] SparseIndices { get; set; }
        public string[] SparseTokens { get; set; }
        public string VectorType { get; set; }
    }

    [Serializable]
    public struct EmbeddingUsage
    {
        public int TotalTokens { get; set; }
    }

    [Serializable]
    public struct EmbeddingResponse
    {
        public string Model { get; set; }
        public string VectorType { get; set; }
        public List<EmbeddingData> Data { get; set; }
        public EmbeddingUsage Usage { get; set; }
    }
    #endregion
}