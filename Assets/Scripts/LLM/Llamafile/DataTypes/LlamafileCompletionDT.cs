using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace LLM
{
    namespace Llamafile
    {
        #region COMMON
        [Serializable]
        public class TokenBias
        {
            public int tokenId;
            public int bias;
        }

        [Serializable]
        public class LogitBias
        {
            public List<TokenBias> tokenBiases;
            public List<List<int>> ToList()
            {
                List<List<int>> biasMatrix = new List<List<int>>();
                foreach (TokenBias tokenBias in tokenBiases)
                {
                    List<int> row = new List<int> { tokenBias.tokenId, tokenBias.bias };
                    biasMatrix.Add(row);
                }
                return biasMatrix;
            }
        }
        #endregion

        #region REQUEST
        public enum MirostatType
        {
            DISABLED = 0,
            MIROSTAT = 1,
            MIROSTAT_2_0 = 2
        }

        public class CompletionRequest : ICompletionRequest
        {
            public string Prompt { get; set; }
            public float Temperature { get; set; } = 0.8f;
            public int TopK { get; set; } = 40;
            public float TopP { get; set; } = 0.95f;
            public float MinP { get; set; } = 0.05f;
            public int NPredict { get; set; } = -1;
            public int NKeep { get; set; } = -1;
            public bool Stream { get; set; } = false;
            public List<string> Stop { get; set; }
            public float TypicalP { get; set; } = 1.0f;
            public float TfsZ { get; set; } = 1.0f;
            public float RepeatPenalty { get; set; } = 1.1f;
            public int RepeatLastN { get; set; } = 64;
            public bool PenalizeNl { get; set; } = true;
            public float PresencePenalty { get; set; } = 0.0f;
            public float FrequencyPenalty { get; set; } = 0.0f;
            public string PenaltyPrompt { get; set; }
            public int Mirostat { get; set; } = (int)MirostatType.DISABLED;
            public float MirostatTau { get; set; } = 5.0f;
            public float MirostatEta { get; set; } = 0.1f;
            public string Grammar { get; set; }
            public Int64 Seed { get; set; } = -1;
            public bool IgnoreEos { get; set; } = false;
            public List<List<int>> LogitBias { get; set; } = new List<List<int>>();
            public int NProbs { get; set; } = 0;
            public int SlotId { get; set; } = -1;
            public bool CachePrompt { get; set; } = false;
        }
        #endregion

        #region RESPONSE
        //[Serializable]
        //public struct GenerationSettings
        //{
        //    public float? FrequencyPenalty { get; set; }
        //    public string Grammar { get; set; }
        //    public bool IgnoreEos { get; set; }
        //    public List<List<int>> LogitBias { get; set; }
        //    public float MinP { get; set; }
        //    public int Mirostat { get; set; }
        //    public float MirostatEta { get; set; }
        //    public float MirostatTau { get; set; }
        //    public string Model { get; set; }
        //    public int NCtx { get; set; }
        //    public int NKeep { get; set; }
        //    public int NPredict { get; set; }
        //    public int NProbs { get; set; }
        //    public bool PenalizeNl { get; set; }
        //    public List<int> PenaltyPromptTokens { get; set; }
        //    public float PresencePenalty { get; set; }
        //    public int RepeatLastN { get; set; }
        //    public float RepeatPenalty { get; set; }
        //    public List<string> Samplers { get; set; }
        //    public Int64 Seed { get; set; }
        //    public List<string> Stop { get; set; }
        //    public bool Stream { get; set; }
        //    public float Temperature { get; set; }
        //    public float TfsZ { get; set; }
        //    public int TopK { get; set; }
        //    public float TopP { get; set; }
        //    public float TypicalP { get; set; }
        //    public bool UsePenaltyPromptTokens { get; set; }
        //}

        [Serializable]
        public struct TokenProbability
        {
            public float Prob { get; set; }
            public string TokStr { get; set; }
        }

        [Serializable]
        public struct CompletionProbabilites
        {
            public string Content { get; set; }
            public List<TokenProbability> Probs { get; set; }
        }

        [Serializable]
        public struct Timings
        {
            public float PredictedMs { get; set; }
            public int PredictedN { get; set; }
            public float PredictedPerSecond { get; set; }
            public float PredictedPerTokenMs { get; set; }
            public float PromptMs { get; set; }
            public int PromptN { get; set; }
            public float PromptPerSecond { get; set; }
            public float PromptPerSecondJart { get; set; }
            public float PromptPerTokenMs { get; set; }
        }

        [Serializable]
        public class CompletionResponse
        {
            public List<CompletionProbabilites> CompletionProbabilities { get; set; }
            public string Content { get; set; }
            public bool Stop { get; set; }
            //public GenerationSettings GenerationSettings { get; set; }
            public string Model { get; set; }
            public string Prompt { get; set; }
            public bool StoppedEos { get; set; }
            public bool StoppedLimit { get; set; }
            public bool StoppedWord { get; set; }
            public string StoppingWord { get; set; }
            public Timings Timings { get; set; }
            public int TokensCached { get; set; }
            public int TokensEvaluated { get; set; }
            public bool Truncated { get; set; }
        }
        #endregion
    }
}
