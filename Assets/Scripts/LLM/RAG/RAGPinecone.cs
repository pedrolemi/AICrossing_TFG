using System;
using System.Collections.Generic;
using UnityEngine;
using Pinecone;
using LLM.PineconeInference;
using PineconeInference;
using System.Threading;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Utilities;

namespace LLM
{
    namespace RAG
    {
        public struct Record
        {
            public string Id { get; set; }
            public string ChunkText { get; set; }
        }

        // Clase que implementa el RAG (Retrieval-Augmented Generation) en Pinecone
        // Se trata de un sistema que permite que los modelos mantengan memoria a largo plazo
        // Para ello se guarda en una base de datos informacion relevante y luego, al interactuar con el modelo
        // se obtiene las respuestas mas similares a la peticion, que se le proporcionan al modelo para que
        // disponga de mas informacion
        public class RAGPinecone : MonoBehaviour
        {
            private const string METADATA_TEXT_FIELD = "chunk_text";

            [SerializeField]
            private string apiKey;
            [SerializeField]
            private string indexName = "aicrossing-index";
            [SerializeField]
            private uint indexDimension = 1024;
            [SerializeField]
            private string namespaceName = "gdd-aicrossing";
            [SerializeField]
            private EmbeddingModel embeddingModel = EmbeddingModel.LLAMA_TEXT_EMBED_V2;
            [SerializeField]
            private RerankingModel rerankingModel = RerankingModel.BGE_RERANKER_V2_M3;
            [SerializeField]
            private int maxChunkSize = 200;
            [SerializeField]
            private int chunkOverlap = 9;
            // Texto que se inserta en la base de datos
            [SerializeField]
            private TextAsset textAsset;

            private PineconeInferenceApi pineconeInferenceApi;
            private PineconeClient pinecone;
            // Indice, es decir, la base de datos donde se guarda el texto anterior
            private Index<Pinecone.Rest.RestTransport> index;

            private CancellationTokenSource cancellationToken;

            private bool working;

            private async void Start()
            {
                TextSplitter recursiveTextSplitter = new TextSplitter();
                List<string> chunks = recursiveTextSplitter.SplitText(textAsset.text);
                List<Record> records = chunks.Select((chunk, index) => new Record
                {
                    ChunkText = chunk,
                    Id = $"chunk{index}"
                }).ToList();

                working = false;
                cancellationToken = new CancellationTokenSource();
                await Load(apiKey);
            }

            public async Task Load(string apiKey)
            {
                if (!string.IsNullOrEmpty(apiKey))
                {
                    this.apiKey = apiKey.Trim();
                    pineconeInferenceApi = new PineconeInferenceApi(this.apiKey);
                    pinecone = new PineconeClient(this.apiKey);
                    // Se obtiene la lista de indices
                    IndexDetails[] indexDetails = await pinecone.ListIndexes();

                    // Se crea el indice si no existe
                    IEnumerable<string> indexDetailsNames = indexDetails.Select(i => i.Name);
                    if (!indexDetailsNames.Contains(indexName))
                    {
                        // La similitud entre vectores es a traves de la distancia del coseno
                        await pinecone.CreateServerlessIndex(indexName, indexDimension, Metric.Cosine, "aws", "us-east-1", cancellationToken.Token);
                    }

                    // Se espera a que se haya terminado de crear
                    index = await pinecone.GetIndex(indexName, cancellationToken.Token);
                    while (!index.Status.IsReady)
                    {
                        await Task.Delay(4000, cancellationToken.Token);
                        index = await pinecone.GetIndex(indexName, cancellationToken.Token);
                    }

                    // Si el indice no existia, se considera que estaba vacio, por lo tanto, hay que rellenarlo con la informacion relevante
                    // De modo esta operacion se realiza una sola vez y luego, ya no es necesario
                    if (!indexDetailsNames.Contains(indexName) && textAsset != null)
                    {
                        // El texto es muy grande, de modo que hay que partirlo en trozos mas pequenos
                        // Es importante este proceso para luego poder obtener los trozos mas relevantes
                        // a la peticion que se haga, que son los que le permitiran al modelo tener mas contexto
                        TextSplitter recursiveTextSplitter = new TextSplitter(maxChunkSize, chunkOverlap);
                        List<string> chunks = recursiveTextSplitter.SplitText(textAsset.text);
                        List<Record> records = chunks.Select((chunk, index) => new Record
                        {
                            ChunkText = chunk,
                            Id = $"chunk{index}"
                        }).ToList();

                        uint nRecords = await UpsertRecordsAsync(records);
                        Debug.Log($"{nRecords} records added to index {indexName}.");
                    }

                    working = true;
                }
            }

            // Se obtienen los embeddings de una serie de textos dados
            private async Task<List<float[]>> EmbeddingAsync(List<TextContainer> inputs, string inputType)
            {
                EmbeddingRequest embeddingRequest = new EmbeddingRequest()
                {
                    Model = embeddingModel.GetDescriptionCached(),
                    Inputs = inputs,
                    Parameters = new EmbeddingParameters()
                    {
                        InputType = inputType,
                        Truncate = TruncateType.END
                    }
                };

                Result<EmbeddingResponse> response = await pineconeInferenceApi.EmbeddingAsync(embeddingRequest, cancellationToken.Token);
                if (response.IsSucess)
                {
                    if (response.Value.VectorType == VectorType.DENSE)
                    {
                        return response.Value.Data.Select(data => data.Values).ToList();
                    }
                }
                return null;
            }

            // Se agregan vectores al indice
            private async Task<uint> UpsertRecordsAsync(List<Record> records)
            {
                List<TextContainer> inputs = records.Select(record => new TextContainer()
                {
                    Text = record.ChunkText
                }).ToList();

                List<float[]> embeddings = await EmbeddingAsync(inputs, InputTypeClass.PASSAGE);
                if (embeddings != null && embeddings.Count > 0)
                {
                    IEnumerable<Vector> vectors = records.Select((record, index) => new Vector()
                    {
                        Id = record.Id,
                        Values = embeddings[index],
                        // Los metadatos contienen el texto al que corresponden,
                        // que es lo que luego se devolvera
                        Metadata = new()
                        {
                            [METADATA_TEXT_FIELD] = record.ChunkText,
                        }
                    });
                    return await index.Upsert(vectors, namespaceName, cancellationToken.Token);
                }
                return 0;
            }

            // Rerankear una serie de textos en base a una peticion, para obtener los mas simialres
            private async Task<List<string>> RerankingAsync(string query, List<JObject> documents, int topN)
            {
                RerankingRequest rerankingRequest = new RerankingRequest()
                {
                    Model = rerankingModel.GetDescriptionCached(),
                    Query = query,
                    Documents = documents,
                    TopN = topN,
                    ReturnDocuments = true,
                    RankFields = new List<string>() { METADATA_TEXT_FIELD },
                    Parameters = new RerankingParameters()
                    {
                        Truncate = TruncateType.END
                    }
                };
                Result<RerankingResponse> response = await pineconeInferenceApi.RerankingAsync(rerankingRequest, cancellationToken.Token);
                if (response.IsSucess)
                {
                    return response.Value.Data.Select(data => data.Document[METADATA_TEXT_FIELD].ToString()).ToList();
                }
                return null;
            }

            // Se realiza una peticion para a traves del sistema de RAG, obtener texto simialres
            public async Task<List<string>> QueryAsync(string query, uint topK, int topN = 0)
            {
                if (working)
                {
                    List<TextContainer> inputs = new List<TextContainer>() { new TextContainer()
                    {
                        Text = query
                    } };

                    // Se obtiene el vector de la peticion dada
                    List<float[]> data = await EmbeddingAsync(inputs, InputTypeClass.QUERY);
                    if (data != null && data.Count > 0)
                    {
                        // A partir del vector de la peticion se obtienen los vectores similares que hay almacenados en el indice
                        // Los textos similares tienen vectores similares
                        ScoredVector[] scoredVectors = await index.Query(data.First(), topK, indexNamespace: namespaceName,
                            includeValues: true, includeMetadata: true, ct: cancellationToken.Token);

                        // Si se ha especificado rerankeo, se realiza
                        if (topN > 0)
                        {
                            // A partir de los textos obtenidos en el paso anterior, se obtiene y se ordenan los mas simialres usando un modelo de rerankeo
                            List<JObject> documents = scoredVectors.Where(scoredVecotr => scoredVecotr.Metadata.ContainsKey(METADATA_TEXT_FIELD))
                                .Select(scoredVector => new JObject()
                                {
                                    [METADATA_TEXT_FIELD] = scoredVector.Metadata[METADATA_TEXT_FIELD].ToString()
                                }).ToList();
                            List<string> responses = await RerankingAsync(query, documents, topN);
                            return responses;
                        }
                        else
                        {
                            // En caso contrario, se devuelven simplemente los textos que corresponden con los vectores obtenidos de la base de datos
                            List<string> responses = scoredVectors.Where(scoredVecotr => scoredVecotr.Metadata.ContainsKey(METADATA_TEXT_FIELD))
                                .Select(scoredVector => scoredVector.Metadata[METADATA_TEXT_FIELD].ToString()).ToList();
                            return responses;
                        }
                    }
                }
                return null;
            }

            public void OnDestroy()
            {
                if (cancellationToken != null)
                {
                    cancellationToken.Cancel();
                    cancellationToken.Dispose();
                }
            }
        }
    }
}