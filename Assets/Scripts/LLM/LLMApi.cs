using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Networking;
using System.Linq;
using System.Threading;
using System.Runtime.CompilerServices;
using Utilities;

namespace LLM
{
    // Clase que proporciona una API para enviar mensajes HTTP a un modelo albergado en un servidor
    public abstract class LLMApi
    {
        private static class ContentType
        {
            public const string APPLICATION_JSON = "application/json";
        }

        // Cabeceras del mensaje HTTP
        private List<(string, string)> requestHeaders;

        public LLMApi()
        {
            requestHeaders = new List<(string, string)>();
            // Indica el formato de respuesta del servidor, es decir, un json
            AddHeader("Content-Type", ContentType.APPLICATION_JSON);
        }

        // Informar acercar del error que se ha producido
        private string GetErrorText(UnityWebRequest webRequest)
        {
            string error = $"Web request failed with error: {webRequest.error}";
            bool canDeserialize = JsonSerializer.TryDeserialize(webRequest.downloadHandler.text, out ApiError apiError);
            if (canDeserialize)
            {
                error += "\n" + apiError.GetErrorInfo();
            }
            UnityEngine.Debug.LogWarning(error);
            return error;
        }

        protected void AddHeader(string name, string value)
        {
            requestHeaders.Add((name, value));
        }

        private void SetHeaders(UnityWebRequest webRequest)
        {
            foreach ((string, string) header in requestHeaders)
            {
                webRequest.SetRequestHeader(header.Item1, header.Item2);
            }
        }

        // Se crea el cuerpo del mensaje HTTP, a partir de una clase serializable
        protected byte[] CreateBody<T>(T request)
        {
            string bodyJson = JsonSerializer.Serialize(request);
            byte[] bodyRaw = new UTF8Encoding().GetBytes(bodyJson);
            return bodyRaw;
        }

        // Enviar un mensaje HTTP
        protected async Task<Result<T>> SendRequest<T>(string path, string method, byte[] bodyRaw, CancellationToken cancellationToken)
        {
            // using permite borrar memoria no gesionada por el sistema a partir del patron Dispose
            using (UnityWebRequest webRequest = new UnityWebRequest(path))
            {
                webRequest.method = method;

                SetHeaders(webRequest);

                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();

                var asyncOperation = webRequest.SendWebRequest();
                // Se espera a que la peticion se complete o se cancele
                while (!asyncOperation.isDone && !cancellationToken.IsCancellationRequested)
                {
                    await Task.Yield();
                }

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    // Se envia el mensaje envuelto en su resultado, que en este caso es de exito
                    T result = JsonSerializer.Deserialize<T>(webRequest.downloadHandler.text);
                    return Result<T>.Success(result);
                }
                else
                {
                    // Se envia el mensaje, indicando que ha fallado
                    string error = GetErrorText(webRequest);
                    return Result<T>.Fail(error);
                }
            }
        }

        // Enviar mensaje HTTP de tipo POST
        protected async Task<Result<T1>> SendPostRequest<T1, T2>(string path, T2 request, CancellationToken cancellationToken)
        {
            byte[] bodyRaw = CreateBody(request);
            return await SendRequest<T1>(path, UnityWebRequest.kHttpVerbPOST, bodyRaw, cancellationToken);
        }

        // Enviar mensaje HTTP de tipo GET
        protected async Task<Result<T>> SendGetRequest<T>(string path, CancellationToken cancellationToken)
        {
            return await SendRequest<T>(path, UnityWebRequest.kHttpVerbGET, null, cancellationToken);
        }

        // Enviar un mensaje HTTP de tipo POST sin stream, es decir, se espera a que el servidor haya generado todo para recibirlo 
        protected async Task<Result<T1>> CompletionAsync<T1, T2>(string path, T2 request, CancellationToken cancellationToken) where T2 : ICompletionRequest
        {
            request.Stream = false;
            return await SendPostRequest<T1, T2>(path, request, cancellationToken);
        }

        public List<T> ConvertStreamResponse<T>(string streamResponse)
        {
            // Todos los mensajes comienzan por data: 
            const string DATA_FIELD = "data: ";
            // El ultimo mensaje se determina porque viene seguido por [DONE]
            const string DONE_BODY = "[DONE]";

            List<T> responses = new List<T>();

            streamResponse = streamResponse.Trim();
            string[] chunks = streamResponse.Split('\n');
            chunks = chunks.Where((chunk) => chunk != "").ToArray();

            foreach (string chunk in chunks)
            {
                // Se trata de un mensaje valido
                if (chunk.StartsWith(DATA_FIELD))
                {
                    // Se elimina el campo
                    string chunkAux = chunk.Replace(DATA_FIELD, "");
                    if (chunkAux != DONE_BODY)
                    {
                        // Se deserializa y se agrega a la lista
                        T responsePart = JsonSerializer.Deserialize<T>(chunkAux);
                        responses.Add(responsePart);
                    }
                }
            }

            return responses;
        }

        // Enviar un mensaje HTTP de tipo POST en stream, es decir, conforme el servidor lo va generando, se puede descargar
        // En modelos de lenguaje, es muy utiles en aplicaciones de chat para que no haya apenas tiempos de espera
        public async IAsyncEnumerable<Result<List<T1>>> CompletionStreamAsync<T1, T2>(string path, T2 request, [EnumeratorCancellation] CancellationToken cancellationToken) where T2 : ICompletionRequest
        {
            request.Stream = true;

            byte[] bodyRaw = CreateBody(request);

            using (UnityWebRequest webRequest = new UnityWebRequest(path))
            {
                webRequest.method = UnityWebRequest.kHttpVerbPOST;

                SetHeaders(webRequest);

                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();

                var asyncOperation = webRequest.SendWebRequest();

                // Se hace yield para asegurar que se ha recibido informacion del servidor
                await Task.Yield();

                float lastProgress = 0.0f;

                while (!asyncOperation.isDone && !cancellationToken.IsCancellationRequested)
                {
                    // Se controla el progreso del mensaje, de modo que cuando ha variado es que hay nueva informacion
                    float currentProgress = webRequest.downloadProgress;
                    if (currentProgress != lastProgress)
                    {
                        // Se convierten las respuestas, que vienen en formato data-only server-sent events
                        List<T1> responses = ConvertStreamResponse<T1>(webRequest.downloadHandler.text);
                        yield return Result<List<T1>>.Success(responses);

                        lastProgress = currentProgress;
                    }

                    await Task.Yield();
                }

                // Cuando se completa, se vuelven a convertir una vez mas todos los mensajes, por si quedaba alguno pendiente
                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    List<T1> responses = ConvertStreamResponse<T1>(webRequest.downloadHandler.text);
                    yield return Result<List<T1>>.Success(responses);
                }
                else
                {
                    // Si se produce un error, se procede igual que en el caso anterior
                    string error = GetErrorText(webRequest);
                    yield return Result<List<T1>>.Fail(error);
                    yield break;
                }
            }
        }

        public string JoinStreamResponses(IEnumerable<string> responses)
        {
            string result = string.Join("", responses);
            result = result.Trim();
            return result;
        }
    }
}