using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace LLM
{
    namespace Groq
    {
        public class GroqApi : LLMApi
        {
            private const string BASE_URL = "https://api.groq.com/openai/v1/";
            private const string CHAT_COMPLETION_ENDPOINT = "chat/completions";
            private const string LIST_MODELS_ENDPOINT = "models";
            private const string RETRIEVE_MODEL = "models/";

            private const string API_KEY_FILE_NAME = "groq_api_key.txt";

            public GroqApi(string apiKey) : base()
            {
                AddHeader("Authorization", $"Bearer {apiKey}");
            }

            private string GetPath(string endpoint)
            {
                return BASE_URL + endpoint;
            }

            public async Task<Result<ChatCompletionResponse>> ChatCompletionAsync(ChatCompletionRequest request, CancellationToken cancellationToken)
            {
                string path = GetPath(CHAT_COMPLETION_ENDPOINT);
                return await CompletionAsync<ChatCompletionResponse, ChatCompletionRequest>(path, request, cancellationToken);
            }

            public async IAsyncEnumerable<Result<List<ChatCompletionResponse>>> ChatCompletionStreamAsync(ChatCompletionRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                string path = GetPath(CHAT_COMPLETION_ENDPOINT);
                await foreach (Result<List<ChatCompletionResponse>> responses in CompletionStreamAsync<ChatCompletionResponse, ChatCompletionRequest>(path, request, cancellationToken))
                {
                    yield return responses;
                    if (!responses.IsSucess)
                    {
                        yield break;
                    }
                }
            }

            public async Task<string> ChatCompletionCallbacksAsync(ChatCompletionRequest request, CancellationToken cancellationToken, Action<string> onHandle, Action<string> onComplete, Action<string> onError)
            {
                string result = "";
                if (request.Stream)
                {
                    await foreach (Result<List<ChatCompletionResponse>> responses in ChatCompletionStreamAsync(request, cancellationToken))
                    {
                        if (responses.IsSucess)
                        {
                            IEnumerable<string> results = responses.Value.Select(result => result.Choices[0].Delta.Content);
                            result = JoinStreamResponses(results);
                            onHandle?.Invoke(result);
                        }
                        else
                        {
                            onError?.Invoke(responses.Error);
                            return result;
                        }
                    }
                }
                else
                {
                    Result<ChatCompletionResponse> response = await ChatCompletionAsync(request, cancellationToken);
                    if (response.IsSucess)
                    {
                        result = response.Value.Choices[0].Message.Content;
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

            private void ProcessTools(ChatCompletionRequest request, List<ToolCall> toolsCalls)
            {
                foreach (ToolCall toolCall in toolsCalls)
                {
                    string functionName = toolCall.Function.Name;
                    string functionArgs = toolCall.Function.Arguments;

                    // Se encuentra la funcion a ejecutar, dependiendo del nombre
                    Tool tool = request.Tools.Find((tool) => tool.Function.Name == functionName);
                    if (tool != null)
                    {
                        // Se parsean los arguemntos
                        JObject args = JObject.Parse(functionArgs);
                        // Se ejecuta la funcion y se obtiene un resultado
                        string functionResponse = tool.Function.Execute(args);
                        // Se agrega un mensaje, con la informacion obtenida de la tool
                        Message message = new Message()
                        {
                            Role = "tool",
                            Content = functionResponse,
                            ToolCallId = toolCall.Id,
                            Name = functionName
                        };
                        request.Messages.Add(message);
                    }
                }
            }

            // El servidor de Groq Cloud implementa la opcion de que el modelo ejecute tooles,
            // que consiste en responder con un json donde se indica la funcion a ejecutar y los argumentos de dicha funcion
            public async Task<Result<ChatCompletionResponse>> ChatCompletionWithToolsAsync(ChatCompletionRequest request, CancellationToken cancellationToken)
            {
                if (request.Tools != null && request.Tools.Count > 0)
                {
                    // Se realiza una peticion comun al modelo
                    Result<ChatCompletionResponse> response = await ChatCompletionAsync(request, cancellationToken);
                    if (response.IsSucess)
                    {
                        Message responseMessage = response.Value.Choices[0].Message;
                        // Si la peticion contiene tools, es que tiene una funcion por ejecutar
                        List<ToolCall> toolCalls = responseMessage.ToolCalls;

                        if (toolCalls != null && toolCalls.Count > 0)
                        {
                            // Se agrega la respuesta del modelo
                            request.Messages.Add(responseMessage);
                            // Se ejecutan las tools correspondientes y se agrega un mensaje que indica la respuesta de la tool
                            ProcessTools(request, toolCalls);
                            // Se vuelve a enviar una peticion, para que el modelo responda, en base a lo obtenido en la tool
                            Result<ChatCompletionResponse> secondResponse = await ChatCompletionAsync(request, cancellationToken);
                            return secondResponse;
                        }
                        return response;
                    }
                    else
                    {
                        return response;
                    }
                }

                return null;
            }

            // Mismo comportamiento que la anteriro, pero en stream
            public async IAsyncEnumerable<Result<List<ChatCompletionResponse>>> ChatCompletionWithToolsStreamAsync(ChatCompletionRequest request, [EnumeratorCancellation] CancellationToken cancellationToken)
            {
                if (request.Tools != null && request.Tools.Count > 0)
                {
                    Dictionary<int, ToolCall> finalToolCalls = new Dictionary<int, ToolCall>();
                    await foreach (Result<List<ChatCompletionResponse>> responses in ChatCompletionStreamAsync(request, cancellationToken))
                    {
                        if (responses.IsSucess)
                        {
                            // Se obtiene los mensajes donde hay tools
                            IEnumerable<ChatCompletionResponse> toolResponses = responses.Value.Where((responsePart) =>
                            {
                                List<ToolCall> toolCalls = responsePart.Choices[0].Delta.ToolCalls;
                                return toolCalls != null && toolCalls.Count > 0;
                            });

                            if (toolResponses.Count() > 0)
                            {
                                // Se agregan las tools a un diccionario, dependiendo de su indice
                                // Como esta en stream, puede llegar por un lado la funcion y por otro los argumentos de la misma
                                foreach (ChatCompletionResponse toolResponse in toolResponses)
                                {
                                    List<ToolCall> toolCalls = toolResponse.Choices[0].Delta.ToolCalls;
                                    if (toolCalls != null && toolCalls.Count > 0)
                                    {
                                        foreach (ToolCall toolCall in toolCalls)
                                        {
                                            int index = toolCall.Index;
                                            // Se agrega la funcion
                                            if (!finalToolCalls.ContainsKey(index))
                                            {
                                                finalToolCalls.Add(index, toolCall);
                                            }
                                            else
                                            {
                                                // Se agregan los argumentos de la funcion correspondiente
                                                finalToolCalls[index].Function.Arguments += toolCall.Function.Arguments;
                                            }
                                        }
                                    }
                                }
                            }
                            else
                            {
                                yield return responses;
                            }
                        }
                        else
                        {
                            yield return responses;
                            yield break;
                        }
                    }

                    if (finalToolCalls != null && finalToolCalls.Count > 0)
                    {
                        // Se procede igual que la vez anterior
                        List<ToolCall> toolCallsList = finalToolCalls.Values.ToList();
                        Message responseMessage = new Message
                        {
                            Role = "assistant",
                            ToolCalls = toolCallsList
                        };
                        request.Messages.Add(responseMessage);
                        ProcessTools(request, toolCallsList);
                        await foreach (Result<List<ChatCompletionResponse>> responses in ChatCompletionStreamAsync(request, cancellationToken))
                        {
                            if (responses.IsSucess)
                            {
                                yield return responses;
                            }
                            else
                            {
                                yield return responses;
                                yield break;
                            }
                        }
                    }
                }
            }

            public async Task<Result<ListModelsResponse>> ListModelsAsync(CancellationToken cancellationToken)
            {
                string path = GetPath(LIST_MODELS_ENDPOINT);
                return await SendGetRequest<ListModelsResponse>(path, cancellationToken);
            }

            public async Task<Result<ModelObject>> RetrieveModelAsync(string model, CancellationToken cancellationToken)
            {
                string path = GetPath(RETRIEVE_MODEL + model);
                return await SendGetRequest<ModelObject>(path, cancellationToken);
            }
        }
    }
}