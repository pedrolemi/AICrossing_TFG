using System;
using System.Collections.Generic;

namespace LLM
{
    namespace Llamafile
    {
        #region REQUEST
        [Serializable]
        public class EmbeddingRequest
        {
            public string Content { get; set; }
        }
        #endregion

        #region RESPONSE
        [Serializable]
        public struct EmbeddingResponse
        {
            public List<float> Embedding { get; set; }
        }
        #endregion
    }
}
