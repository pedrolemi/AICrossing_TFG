using System;
using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System.Linq;
using UnityEditor;
using System.Threading;
using Utilities;

namespace LLM
{
    public enum LLMLocation
    {
        REMOTE_SERVER,
        LOCAL_SERVER,
        LOCAL
    }

    // Clase que agrupa las 3 opcones (remoto, servidor local y local) y permite varias entre ellas
    // Ademas implementa el RAG en las conversaciones
    public class LLMProvider : MonoBehaviour
    {
        [TextArea(5, 10)]
        [SerializeField]
        private string systemMessage;

        [Header("Basic Options")]
        [SerializeField]
        private LLMLocation llmLocation = LLMLocation.REMOTE_SERVER;
        public LLMLocation LLMLocation { get => llmLocation; set => llmLocation = value; }
        [Tooltip("Si esta activado, se recibe la respuesta conforme el modelo la crea.\n" +
            "En caso contrario, se recibe de una sola vez.")]
        [SerializeField]
        private bool stream = true;
        [Tooltip("La temperatura determina como de aleatoria es la respusta generada por el modelo.\n" +
            "Valores mas altos producen respuestas mas aleatorias.\n" +
            "Valores mas bajos producen respuestas mas centradas y deterministas.\n" +
            "Se recomienda variar este valor o Top P, pero no ambos a la vez.")]
        [Range(0.0f, 2.0f)]
        [SerializeField]
        private float temperature = 1.0f;
        [Tooltip("El numero maximo de tokens que pueden ser generados por el modelo.\n" +
            "-1 = maximo numero de tokens permitido por el contexto del modelo")]
        [SerializeField]
        private int maxCompletionTokens = 256;


        [Header("Remote Server & Local Server Options")]
        [Tooltip("Controla la probabilidad acumulativa de generar tokens," +
            "de modo que el modelo creara tokens hasta alcanzar el umbral P.\n" +
            "Reducir este valor supone respuestas mas cortas y diversas.\n" +
            "Se recomienda variar este valor o Temperature, pero no ambos a la vez.")]
        [Range(0f, 1f)]
        [SerializeField]
        private float topP = 1.0f;
        [Tooltip("Los valores positivos penalizan los nuevos tokens basandose en su existencia en el texto ya generado," +
            "de modo que reducen la probabilidad de que el modelo repita el mismo texto.")]
        [Range(-2.0f, 2.0f)]
        [SerializeField]
        private float frequencyPenalty = 0.0f;
        [Tooltip("Los valores positivos penalizan los nuevos tokens basandose en su existencia en el texto ya generado," +
            "de modo que incrementan la posibilidad de que el modelo hable de nuevos temas.")]
        [Range(-2.0f, 2.0f)]
        [SerializeField]
        private float presencePenalty = 0.0f;
        [Tooltip("Semilla para reproducir la misma respuesta.\n" +
            "-1 = resultados aleatorias cada vez que se hace una peticion al modelo.")]
        [SerializeField]
        private int seed = -1;
        [Tooltip("Array de hasta 4 palabras para parar al modelo y hacer que deje de generar nuevo contenido.")]
        [SerializeField]
        private List<string> stop;

        [Header("Remote Server Options")]
        [SerializeField]
        private string apiKey = "";
        [Tooltip("El ID del modelo que usar.")]
        [SerializeField]
        private Groq.Model model = Groq.Model.LLAMA_3_3_70B_VERSATILE;

        [Header("Local Server & Local Options")]
        [Tooltip("Probabilidad minima de un token para ser elegido.")]
        [Range(0f, 1f)]
        [SerializeField]
        private float minP = 0.05f;

        [Header("Local Server Options")]
        [SerializeField]
        private Llamafile.LlamafileModel llamafile;
        [Tooltip("Limita el siguiente token elegido a los K tokens mas probables. " +
            "Valores mas altos generan respuestas mas diversas, mientras que valores mas bajos, respuestas mas especficas.")]
        [Range(-1, 100)]
        [SerializeField]
        private int topK = 40;
        [Tooltip("Controla la repeticion de secuencias de tokens en el texto generado.")]
        [Range(0f, 2f)]
        [SerializeField]
        private float repeatPenalty = 1.1f;
        [Tooltip("Ultimos N tokens que considerar para la \"penalize repetition\".")]
        [Range(0, 2048)]
        [SerializeField]
        private int repeatLastN = 64;
        [Tooltip("Penaliza tokens de salto de linea cuando se aplica \"repeat penalty\".")]
        [SerializeField]
        private bool penalizeNl = true;
        [Tooltip("Prompt que se utiliza para el calculo del \"penalty\".\n" +
            "null = se usa el prompt original para realizar este calculo.")]
        [SerializeField]
        private string penaltyPrompt = null;
        [Tooltip("Habilita el sampleo de tokens Mirostat, controlando la perplejidad durante la generacion de texto.\n")]
        [SerializeField]
        private Llamafile.MirostatType mirostat = Llamafile.MirostatType.DISABLED;
        [Range(0f, 10f)]
        [Tooltip("Establece la entropia objetivo Mirostat (tau), que controla el balance entre la coherencia y la diversidad en el texto generado.")]
        [SerializeField]
        private float mirostatTau = 5.0f;
        [Range(0f, 1f)]
        [Tooltip("Establece el learning rate Mirostat (eta), que controla como de rapido el algoritmo responde a la retroalimientacion del texto generado.")]
        [SerializeField]
        private float mirostatEta = 0.1f;
        [Tooltip("Si es mayor que 0, la respuesta contiene la probabilidad de los N tokens mejores para cada token generado.")]
        [Range(0, 10)]
        [SerializeField]
        private int nProbs = 0;
        [Tooltip("Se ignora el EOS token y se continua generando.")]
        [SerializeField]
        private bool ignoreEos = false;
        [Tooltip("Especifica el numero de tokens del prompt que conservar cuando se supera el tamano de la ventana dde context y se necesitan descartar tokens." +
            "-1 = conservar todos los tokens del promot.")]
        [SerializeField]
        private int nKeep = -1;
        [Tooltip("Modifica la probabilidad de que uno o varios tokens aparezcan en el texto generado.\n" +
            "Para ello se asigna un un token ID y un bias positivo o negativo, que indica la probabilidad de aparecer de dicho token.")]
        [SerializeField]
        private Llamafile.LogitBias logitBias;
        [Tooltip("Archivo .gbnf que define una gramatica y se utiliza para restringir el texto generado por el modelo a un formato especifico.")]
        [SerializeField]
        private TextAsset grammar;
        [Tooltip("Establece el valor del parametro Z que se usa en el sampleo \"tail free\"." +
            "-1.0 = desabilitado")]
        [SerializeField]
        private float trailFreeSampling = 1.0f;

        [Header("Local Options")]
        [SerializeField]
        private LlamaCpp.LlamaCppModel llamaCpp;
        [SerializeField]
        private int MinKeep = 1;

        [Header("RAG Options")]
        [SerializeField]
        private RAG.RAGPinecone ragPinecone;
        [Tooltip("Numero de respuestas que obtener de la base de datos de vectores densos.")]
        [SerializeField]
        private uint ragTopK = 4;
        [Tooltip("Numero de respuestas que obtener del rerankea de las peticiones obtenidas de la base de datos.")]
        [SerializeField]
        private int ragTopN = 3;

        public ChatBufferMemory ChatMemory { get; private set; }

        private Groq.GroqApi groqApi;

        private Llamafile.LlamafileApi llamafileApi;
        // La gramatica define en LlamaCpp el formato de respuesta del modelo
        private string grammarString;
        private string jsonGrammarString;

        private LlamaCpp.LlamaStatelessInference statelessInference;
        private LlamaCpp.LlamaInteractiveInference interactiveInference;

        private CancellationTokenSource cancellationToken;

        private Dictionary<string, string> replacements;

        private void Awake()
        {
            ChatMemory = new ChatBufferWindowMemory(systemMessage, 5);
        }

        private void Start()
        {
            cancellationToken = new CancellationTokenSource();
            replacements = new Dictionary<string, string>();

            LoadGroq(apiKey, model);
            LoadLlamafile(llamafile);
            LoadLlamaCpp(llamaCpp);
            LoadPinecone(ragPinecone);
        }

        public void LoadGroq(string apiKey, Groq.Model model)
        {
            if (!string.IsNullOrEmpty(apiKey))
            {
                this.apiKey = apiKey.Trim();
                this.model = model;
                groqApi = new Groq.GroqApi(apiKey);
            }
        }

        public void LoadLlamafile(Llamafile.LlamafileModel llamafile)
        {
            if (llamafile != null)
            {
                this.llamafile = llamafile;
                llamafileApi = new Llamafile.LlamafileApi();
                grammarString = grammar != null ? grammar.text : null;
                TextAsset jsonGrammar = Resources.Load<TextAsset>("json_arr");
                jsonGrammarString = jsonGrammar.text;
            }
        }

        public void LoadLlamaCpp(LlamaCpp.LlamaCppModel llamaCpp)
        {
            if (llamaCpp != null)
            {
                this.llamaCpp = llamaCpp;
                this.llamaCpp.AddLoadedModelListener(() =>
                {
                    statelessInference = llamaCpp.CreateStatelessInference();
                    interactiveInference = llamaCpp.CreateInteractiveInference();
                });
            }
        }

        public void LoadPinecone(RAG.RAGPinecone ragPinecone)
        {
            if (ragPinecone != null)
            {
                this.ragPinecone = ragPinecone;
            }
        }

        public void SetSystemMessage(string systemMessage)
        {
            this.systemMessage = systemMessage;
            ChatMemory.SetSystemMessage(this.systemMessage);
        }

        private Groq.ChatCompletionRequest GenerateGroqRequest(ChatBufferMemory chatHistory, Groq.ResponseType responseFormat = Groq.ResponseType.TEXT, List<Groq.Tool> tools = null, Dictionary<string, string> replacements = null)
        {
            Groq.ChatCompletionRequest request = new Groq.ChatCompletionRequest()
            {
                FrequencyPenalty = frequencyPenalty,
                MaxCompletionTokens = maxCompletionTokens == -1 ? null : maxCompletionTokens,
                Model = model.GetDescriptionCached(),
                Messages = chatHistory.GetGroqMessages(replacements),
                PresencePenalty = presencePenalty,
                ResponseFormat = new Groq.ResponseFormat()
                {
                    Type = responseFormat.GetDescriptionCached(),
                },
                Seed = seed == -1 ? null : seed,
                Stop = stop,
                Stream = stream,
                Temperature = temperature,
                Tools = tools,
                TopP = topP
            };
            return request;
        }

        private List<string> GetStopWords(ChatTemplate chatTemplate)
        {
            List<string> templateStopWords = chatTemplate.GetStopWords();
            if (stop != null)
            {
                templateStopWords.AddRange(stop);
            }
            return templateStopWords;
        }

        private Llamafile.CompletionRequest GenerateLlamafileRequest(ChatBufferMemory chatHistory, ChatTemplate chatTemplate, bool cachePrompt, string grammar, Dictionary<string, string> replacements = null)
        {
            string prompt = chatHistory.FormatPrompt(chatTemplate, true, false, replacements);
            Llamafile.CompletionRequest request = new Llamafile.CompletionRequest()
            {
                Prompt = prompt,
                Temperature = temperature,
                TopK = topK,
                MinP = minP,
                NPredict = maxCompletionTokens,
                NKeep = nKeep,
                Stream = stream,
                Stop = GetStopWords(chatTemplate),
                TfsZ = trailFreeSampling,
                RepeatPenalty = repeatPenalty,
                RepeatLastN = repeatLastN,
                PenalizeNl = penalizeNl,
                PresencePenalty = presencePenalty,
                FrequencyPenalty = frequencyPenalty,
                PenaltyPrompt = penaltyPrompt,
                Mirostat = (int)mirostat,
                MirostatTau = mirostatTau,
                MirostatEta = mirostatEta,
                Grammar = grammar,
                Seed = seed,
                IgnoreEos = ignoreEos,
                LogitBias = logitBias.ToList(),
                NProbs = nProbs,
                CachePrompt = cachePrompt
            };
            return request;
        }

        private LlamaCpp.InferenceParams GenerateLlamaCppRequest(Dictionary<string, string> replacements = null)
        {
            LlamaCpp.DefaultSamplingChain samplingChain = new LlamaCpp.DefaultSamplingChain()
            {
                Temperature = temperature,
                MinP = minP,
                MinKeep = MinKeep,
            };
            samplingChain.Seed = seed > -1 ? (uint)seed : samplingChain.Seed;

            LlamaCpp.InferenceParams inferenceParams = new LlamaCpp.InferenceParams()
            {
                MaxTokens = maxCompletionTokens,
                SamplingChain = samplingChain,
                Replacements = replacements
            };
            return inferenceParams;
        }

        // Enviar una peticion con tools (solo en remoto)
        public async Task<string> ChatCompletionWithTools(string query, Action<string> onHandle, Action<string> onComplete, Action<string> onError, List<Groq.Tool> tools, Groq.ToolChoiceType toolChoiceType, Dictionary<string, string> replacements = null)
        {
            string result = "";

            if (llmLocation == LLMLocation.REMOTE_SERVER && groqApi != null)
            {
                ChatMemory.AddUserMessage(query);

                Groq.ChatCompletionRequest request = GenerateGroqRequest(ChatMemory, Groq.ResponseType.TEXT, tools, replacements);
                request.ToolChoice = toolChoiceType.GetDescriptionCached();
                request.ParallelToolCalls = false;
                if (stream)
                {
                    await foreach (Result<List<Groq.ChatCompletionResponse>> responses in groqApi.ChatCompletionWithToolsStreamAsync(request, cancellationToken.Token))
                    {
                        if (responses.IsSucess)
                        {
                            IEnumerable<string> results = responses.Value.Select(result => result.Choices[0].Delta.Content);
                            result = groqApi.JoinStreamResponses(results);
                            onHandle?.Invoke(result);
                        }
                        else
                        {
                            ChatMemory.RemoveLastMessage();
                            onError?.Invoke(responses.Error);
                            return result;
                        }
                    }
                }
                else
                {
                    Result<Groq.ChatCompletionResponse> response = await groqApi.ChatCompletionWithToolsAsync(request, cancellationToken.Token);
                    if (response.IsSucess)
                    {
                        result = response.Value.Choices[0].Message.Content;
                        result = result.Trim();
                        onHandle?.Invoke(result);
                    }
                    else
                    {
                        ChatMemory.RemoveLastMessage();
                        onError?.Invoke(response.Error);
                        return result;
                    }
                }

                if (!string.IsNullOrEmpty(result))
                {
                    ChatMemory.AddAssistantMessage(result);
                }

                onComplete?.Invoke(result);
            }
            else
            {
                onError?.Invoke("Unable to execute the model.");
            }

            return result;
        }

        // A partir de una peticion, obtener las respuestas mas similares de la base de datos
        private async Task<List<string>> RAGCompletion(string query)
        {
            Dictionary<string, string> replacements = new Dictionary<string, string>();
            ChatBufferMemory chatMemoryAux = ChatMemory.Clone() as ChatBufferMemory;

            // Se usa un modelo de lenguaje para condensar toda la conversacion en una sola pregunta
            string systemMessage = @"Eres un asistente que, basándose en un historial de chat y el último mensaje del usuario, debes reformular la última pregunta del usuario para que se entienda claramente sin necesidad del contexto del historial.
Si la pregunta es comprensible tal como está, devuélvela sin cambios. Si no, reformúlala para mejorar su claridad.
DEVUELVE ÚNICAMENTE LA PREGUNTA, SIN NINGÚN COMENTARIO ADICIONAL";
            chatMemoryAux.SetSystemMessage(systemMessage);

            chatMemoryAux.AddUserMessage($@"El último mensaje del usuario es: {query}.
Reformula este mensaje para que sea claro y se entienda sin necesidad de consultar el historial de chat anterior.");

            const int MAX_TOKENS_SUMMARY = 50;

            if (llmLocation == LLMLocation.REMOTE_SERVER && groqApi != null)
            {
                Groq.ChatCompletionRequest request = GenerateGroqRequest(chatMemoryAux);
                request.MaxCompletionTokens = MAX_TOKENS_SUMMARY;
                Result<Groq.ChatCompletionResponse> response = await groqApi.ChatCompletionAsync(request, cancellationToken.Token);
                if (response.IsSucess)
                {
                    // Se obtienen las respuestas mas similares
                    return await ragPinecone.QueryAsync(response.Value.Choices[0].Message.Content.Trim(), ragTopK, ragTopN);
                }
            }
            else if (llmLocation == LLMLocation.LOCAL_SERVER && llamafile != null)
            {
                Llamafile.CompletionRequest request = GenerateLlamafileRequest(chatMemoryAux, llamafile.ChatTemplate, false, null);
                request.NPredict = MAX_TOKENS_SUMMARY;
                Result<Llamafile.CompletionResponse> response = await llamafileApi.CompletionAsync(request, cancellationToken.Token);
                if (response.IsSucess)
                {
                    // Se obtienen las respuestas mas similares
                    return await ragPinecone.QueryAsync(response.Value.Content.Trim(), ragTopK, ragTopN);
                }
            }
            else if (llmLocation == LLMLocation.LOCAL && statelessInference != null)
            {
                LlamaCpp.InferenceParams inferenceParams = GenerateLlamaCppRequest();
                inferenceParams.MaxTokens = MAX_TOKENS_SUMMARY;
                string response = await llamaCpp.RunAsync(statelessInference, chatMemoryAux, inferenceParams, cancellationToken.Token, null, null, false);
                return await ragPinecone.QueryAsync(response, ragTopK, ragTopN);
            }
            return null;

        }

        // Enviar una peticion, teniendo en cuenta la conversacion pasada
        public async Task<string> ChatCompletion(string query, Action<string> onHandle, Action<string> onComplete, Action<string> onError, Dictionary<string, string> replacements = null, bool enableRag = true)
        {
            //Se incluye en la pipleine el RAG
            if (enableRag && ragPinecone != null && llmLocation != LLMLocation.LOCAL)
            {
                if (replacements == null)
                {
                    this.replacements.Clear();
                    replacements = this.replacements;
                }
                // Se obtienen las respuestas mas similares, que posteriormente se reemplazaran
                List<string> contextualResponses = await RAGCompletion(query);
                if (contextualResponses != null && contextualResponses.Count > 0)
                {
                    replacements["context"] = string.Join("\n", contextualResponses);
                }
            }

            ChatMemory.AddUserMessage(query);

            Action<string> onCompleteAux = result =>
            {
                if (!string.IsNullOrEmpty(result))
                {
                    ChatMemory.AddAssistantMessage(result);
                }
                onComplete?.Invoke(result);
            };

            // Si se produce un error, hay que eliminar el ultimo mensaje, ya que no ha habido respuesta
            Action<string> onErrorAux = error =>
            {
                ChatMemory.RemoveLastMessage();
                onError?.Invoke(error);
            };

            string result = "";
            if (llmLocation == LLMLocation.REMOTE_SERVER && groqApi != null)
            {
                Groq.ChatCompletionRequest request = GenerateGroqRequest(ChatMemory, Groq.ResponseType.TEXT, null, replacements);
                result = await groqApi.ChatCompletionCallbacksAsync(request, cancellationToken.Token, onHandle, onCompleteAux, onErrorAux);
            }
            else if (llmLocation == LLMLocation.LOCAL_SERVER && llamafile != null)
            {
                Llamafile.CompletionRequest request = GenerateLlamafileRequest(ChatMemory, llamafile.ChatTemplate, true, grammarString, replacements);
                result = await llamafileApi.CompletionCallbacksAsync(request, cancellationToken.Token, onHandle, onCompleteAux, onErrorAux);
            }
            else if (llmLocation == LLMLocation.LOCAL && interactiveInference != null)
            {
                LlamaCpp.InferenceParams inferenceParams = GenerateLlamaCppRequest(replacements);
                result = await llamaCpp.RunAsync(interactiveInference, ChatMemory, inferenceParams, cancellationToken.Token, onHandle, onComplete, stream);
            }
            else
            {
                onError?.Invoke("Unable to execute the model.");
            }

            return result;
        }

        // Enviar una peticion al modelo aislada, es decir, que no registra nada de la conversacion pasada
        public async Task<string> Completion(ChatBufferMemory chatMemory, Action<string> onHandle, Action<string> onComplete, Action<string> onError, bool stream, int maxCompletionTokens)
        {
            string result = "";
            if (llmLocation == LLMLocation.REMOTE_SERVER && groqApi != null)
            {
                Groq.ChatCompletionRequest request = GenerateGroqRequest(chatMemory);
                request.Stream = stream;
                request.MaxCompletionTokens = maxCompletionTokens;
                result = await groqApi.ChatCompletionCallbacksAsync(request, cancellationToken.Token, onHandle, onComplete, onError);
            }
            else if (llmLocation == LLMLocation.LOCAL_SERVER && llamafile != null)
            {
                Llamafile.CompletionRequest request = GenerateLlamafileRequest(chatMemory, llamafile.ChatTemplate, false, grammarString);
                request.Stream = stream;
                request.NPredict = maxCompletionTokens;
                result = await llamafileApi.CompletionCallbacksAsync(request, cancellationToken.Token, onHandle, onComplete, onError);
            }
            else if (llmLocation == LLMLocation.LOCAL && statelessInference != null)
            {
                LlamaCpp.InferenceParams inferenceParams = GenerateLlamaCppRequest();
                inferenceParams.MaxTokens = maxCompletionTokens;
                result = await llamaCpp.RunAsync(statelessInference, chatMemory, inferenceParams, cancellationToken.Token, onHandle, onComplete, stream);
            }
            else
            {
                onError?.Invoke("Unable to execute the model.");
            }

            return result;
        }

        // Enviar una peticion al modelo aislada, que tiene como respuesta un json estructurado
        public async Task<T> JsonCompletion<T>(ChatBufferMemory chatHistory, Action<T> onComplete, Action<string> onError) where T : new()
        {
            string result = "";
            if (llmLocation == LLMLocation.REMOTE_SERVER && groqApi != null)
            {
                Groq.ChatCompletionRequest request = GenerateGroqRequest(chatHistory, Groq.ResponseType.JSON_OBJECT);
                request.Stream = false;
                Result<Groq.ChatCompletionResponse> response = await groqApi.ChatCompletionAsync(request, cancellationToken.Token);
                if (response.IsSucess)
                {
                    result = response.Value.Choices[0].Message.Content;
                    result = result.Trim();
                }
                else
                {
                    onError?.Invoke(response.Error);
                    return default;
                }
            }
            else if (llmLocation == LLMLocation.LOCAL_SERVER && llamafile != null)
            {
                Llamafile.CompletionRequest request = GenerateLlamafileRequest(chatHistory, llamafile.ChatTemplate, false, jsonGrammarString);
                request.Stream = false;
                Result<Llamafile.CompletionResponse> response = await llamafileApi.CompletionAsync(request, cancellationToken.Token);
                if (response.IsSucess)
                {
                    result = response.Value.Content;
                    result = result.Trim();
                }
                else
                {
                    onError?.Invoke(response.Error);
                    return default;
                }
            }
            else
            {
                onError?.Invoke("Unable to execute the model.");
                return default;
            }

            if (JsonSerializer.TryDeserialize(result, out T jsonObject))
            {
                onComplete?.Invoke(jsonObject);
            }
            else
            {
                onError?.Invoke("Unable to deserialize JSON data.");
            }
            return jsonObject;
        }

        public void Cancel()
        {
            cancellationToken.Cancel();
        }

        private void OnDestroy()
        {
            if (cancellationToken != null)
            {
                cancellationToken.Cancel();
                cancellationToken.Dispose();
            }
        }
    }
}