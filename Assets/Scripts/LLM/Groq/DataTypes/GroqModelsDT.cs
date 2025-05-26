using System;
using System.Collections.Generic;

namespace LLM
{
    #region RESPONSE
    [Serializable]
    public class ModelObject
    {
        public string Id { get; set; }
        public string Object { get; set; }
        public int Created { get; set; }
        public string OwnedBy { get; set; }
        public bool Active { get; set; }
        public int ContextWindow { get; set; }
    }

    [Serializable]
    public class ListModelsResponse
    {
        public string Object { get; set; }
        public List<ModelObject> Data { get; set; }
    }
    #endregion
}