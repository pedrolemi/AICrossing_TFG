using System;
using System.Collections.Generic;

namespace LLM
{
    namespace Llamafile
    {
        #region REQUEST
        [Serializable]
        public class TokenizeRequest
        {
            public string Content { get; set; }
        }
        #endregion

        #region RESPONSE
        [Serializable]
        public struct TokenizeResponse
        {
            public List<int> Tokens { get; set; }
        }
        #endregion
    }
}
