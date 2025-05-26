using System.Collections.Generic;
using System;
using Newtonsoft.Json.Linq;
using System.ComponentModel;

namespace PineconeInference
{
    #region REQUEST
    [Serializable]
    public class RerankingParameters
    {
        public string Truncate { get; set; }
    }

    public enum RerankingModel
    {
        [Description("pinecone-rerank-v0")]
        PINECONE_RERANK_V0,
        [Description("bge-reranker-v2-m3")]
        BGE_RERANKER_V2_M3,
    }

    [Serializable]
    public class RerankingRequest
    {
        public string Model { get; set; }
        public string Query { get; set; }
        public List<JObject> Documents { get; set; }
        public int TopN { get; set; }
        public bool ReturnDocuments { get; set; } = true;
        public List<string> RankFields { get; set; } = new List<string>() { "text" };
        public RerankingParameters Parameters { get; set; }

    }
    #endregion

    #region RESPONSE
    public struct RerankingUsage
    {
        public int RerankUnits { get; set; }
    }

    public struct RerankingData
    {
        public int Index { get; set; }
        public float Score { get; set; }
        public JObject Document { get; set; }
    }

    public struct RerankingResponse
    {
        public string Model { get; set; }
        public List<RerankingData> Data { get; set; }
        public RerankingUsage Usage { get; set; }
    }
    #endregion
}