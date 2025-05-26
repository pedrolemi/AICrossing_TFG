using System.Threading.Tasks;
using System.Threading;
using PineconeInference;

namespace LLM
{
    namespace PineconeInference
    {
        public class PineconeInferenceApi : LLMApi
        {
            private const string BASE_URL = "https://api.pinecone.io/";
            private const string EMBEDDING_ENDPOINT = "embed";
            private const string RERANKING_ENDPOINT = "rerank";

            public PineconeInferenceApi(string apiKey) : base()
            {
                AddHeader("Api-Key", apiKey);
                AddHeader("X-Pinecone-API-Version", "2025-01");
            }

            private string GetPath(string endpoint)
            {
                return BASE_URL + endpoint;
            }

            // Ejecutar un modelo de embeddings, que permtie obtener la representacion vectorial de un texto,
            // que alberga las relaciones semanticas con el cotnexto
            public async Task<Result<EmbeddingResponse>> EmbeddingAsync(EmbeddingRequest request, CancellationToken cancellationToken)
            {
                string path = GetPath(EMBEDDING_ENDPOINT);
                return await SendPostRequest<EmbeddingResponse, EmbeddingRequest>(path, request, cancellationToken);
            }

            // Ejecutar un model de rerankeo, que dado una serie de oraciones y una peticion, ordena las oraciones
            // segun su relacion con la peticion
            public async Task<Result<RerankingResponse>> RerankingAsync(RerankingRequest request, CancellationToken cancellationToken)
            {
                string path = GetPath(RERANKING_ENDPOINT);
                return await SendPostRequest<RerankingResponse, RerankingRequest>(path, request, cancellationToken);
            }
        }
    }
}