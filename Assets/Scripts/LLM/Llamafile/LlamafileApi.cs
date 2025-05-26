using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace LLM
{
    namespace Llamafile
    {
        public static class Host
        {
            public const string DEFAULT = "127.0.0.1";
        }

        public static class Port
        {
            public const int GENERATION_DEFAULT = 8080;
            public const int EMBEDDING_DEFAULT = 8081;
        }

        public class LlamafileApi : LLMApi
        {
            private const string TOKENIZE_ENDPOINT = "tokenize";
            private const string EMBEDDING_ENDPOINT = "embedding";
            private const string COMPLETION_ENDPOINT = "completion";

            private string GetPath(string host, int port, string endpoint)
            {
                return $"http://{host}:{port}/{endpoint}";
            }

            public async Task<Result<TokenizeResponse>> TokenizeAsync(TokenizeRequest request, CancellationToken cancellationToken, string host = Host.DEFAULT, int port = Port.GENERATION_DEFAULT)
            {
                string path = GetPath(host, port, TOKENIZE_ENDPOINT);
                return await SendPostRequest<TokenizeResponse, TokenizeRequest>(path, request, cancellationToken);
            }

            public async Task<Result<EmbeddingResponse>> EmbeddingAsync(EmbeddingRequest request, CancellationToken cancellationToken, string host = Host.DEFAULT, int port = Port.EMBEDDING_DEFAULT)
            {
                string path = GetPath(host, port, EMBEDDING_ENDPOINT);
                return await SendPostRequest<EmbeddingResponse, EmbeddingRequest>(path, request, cancellationToken);
            }

            public async Task<Result<CompletionResponse>> CompletionAsync(CompletionRequest request, CancellationToken cancellationToken, string host = Host.DEFAULT, int port = Port.GENERATION_DEFAULT)
            {
                string path = GetPath(host, port, COMPLETION_ENDPOINT);
                return await CompletionAsync<CompletionResponse, CompletionRequest>(path, request, cancellationToken);
            }

            public async IAsyncEnumerable<Result<List<CompletionResponse>>> CompletionStreamAsync(CompletionRequest request, [EnumeratorCancellation] CancellationToken cancellationToken, string host = Host.DEFAULT, int port = Port.GENERATION_DEFAULT)
            {
                string path = GetPath(host, port, COMPLETION_ENDPOINT);
                await foreach (Result<List<CompletionResponse>> responses in CompletionStreamAsync<CompletionResponse, CompletionRequest>(path, request, cancellationToken))
                {
                    yield return responses;
                    if (!responses.IsSucess)
                    {
                        yield break;
                    }
                }
            }

            public async Task<string> CompletionCallbacksAsync(CompletionRequest request, CancellationToken cancellationToken, Action<string> onHandle, Action<string> onComplete, Action<string> onError, string host = Host.DEFAULT, int port = Port.GENERATION_DEFAULT)
            {
                string result = "";
                if (request.Stream)
                {
                    await foreach (Result<List<CompletionResponse>> responses in CompletionStreamAsync(request, cancellationToken, host, port))
                    {
                        if (responses.IsSucess)
                        {
                            IEnumerable<string> results = responses.Value.Select(result => result.Content);
                            result = JoinStreamResponses(results);
                            onHandle?.Invoke(result);
                        }
                        else
                        {
                            onError?.Invoke(result);
                            return result;
                        }
                    }
                }
                else
                {
                    Result<CompletionResponse> response = await CompletionAsync(request, cancellationToken, host, port);
                    if (response.IsSucess)
                    {
                        result = response.Value.Content;
                        result = result.Trim();
                        onHandle?.Invoke(result);
                    }
                    else
                    {
                        onError?.Invoke(response.Error);
                        return result;
                    }
                }
                onComplete?.Invoke(result);
                return result;
            }
        }
    }
}
