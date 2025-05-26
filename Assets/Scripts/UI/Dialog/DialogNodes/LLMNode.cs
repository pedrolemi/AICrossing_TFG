using LLM;
using Newtonsoft.Json.Linq;
using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Events;

// Nodo que tiene como funcion generar otros nodos, para llevar a cabo una conversacion
[CreateAssetMenu(fileName = "LLM Node", menuName = "Dialog Nodes/LLM Node")]
public class LLMNode : DialogNode
{
    [TextArea(5, 10)]
    public string query;
    public bool useTools = false;
    public bool enableRag = true;
    public bool avoidQuestions = false;
    public float questionProbability = 30.0f;

    private Dictionary<string, string> replacements = new Dictionary<string, string>();

    private const string END_CONVER_KEY = "END_CONVERSATION";
    private const string KEEP_CONVER_KEY = "KEEP_CONVERSATION_GOING";

    private void DecideEndConversation(LLMProvider provider, Action<string> onComplete, Action<string> onError)
    {
        string systemMessage = @"Eres un asistente que debe analizar una frase para determinar si una conversación debe continuar o terminar. 
Tu tarea es decidir si, dada la frase proporcionada, la interacción puede concluir o si es necesario seguir conversando. Asegúrate de considerar el contexto y el tono de la frase al tomar tu decisión.";
        ChatBufferMemory chatHistory = new ChatBufferMemory(systemMessage);

        string userMessage = $@"Dada la siguiente frase, determina si la conversación debe finalizar o continuar.
Si la conversación debe finalizar, responde con {END_CONVER_KEY}.
Si la conversación puede continuar, responde con {KEEP_CONVER_KEY}.
Consulta del usuario: {query}
Respuesta: ";
        chatHistory.AddUserMessage(userMessage);

        _ = provider.Completion(chatHistory, null, onComplete, onError, false, 10);
    }

    private void BaseGenerateNextNodeWithTools(TextNode nextNode, LLMProvider provider, DialogBox dialogBox)
    {
        JObject parameters = new JObject
        {
            ["type"] = "object",
            ["properties"] = new JObject { },
            ["required"] = new JArray { }
        };

        // Se especifica una tool, que se especifica que se ejecute cuando el modelo crea
        // que la conversacion ha terminado
        LLM.Groq.Tool tool = new LLM.Groq.Tool
        {
            Function = new LLM.Groq.ToolFunction()
            {
                Name = "Goodbye",
                Description = "Finaliza cuando el jugador expresa su intención de despedirse o irse.",
                Parameters = parameters,
                Execute = (jsonObject) =>
                {
                    nextNode.canAnswer = false;
                    dialogBox.SetCanAnswer(nextNode.canAnswer);
                    return "Éxito";
                }
            }
        };

        // Se realiza la petiicon
        List<LLM.Groq.Tool> tools = new List<LLM.Groq.Tool> { tool };
        _ = provider.ChatCompletionWithTools(query, dialogBox.SetText, dialogBox.EndSetText, error => dialogBox.EndSetText(null), tools, LLM.Groq.ToolChoiceType.AUTO);
    }

    private void BaseGenerateNextNodeWithoutTools(TextNode nextNode, LLMProvider provider, Personality personality, DialogBox dialogBox)
    {
        Action<string> onError = error => dialogBox.EndSetText(null);

        // Se lleva a cabo una peticion que determina si la conversacion tiene que terminar o no, en base a la palabra clave que genera
        DecideEndConversation(provider, (end) =>
        {
            if (end.Contains(END_CONVER_KEY))
            {
                nextNode.canAnswer = false;
                dialogBox.SetCanAnswer(nextNode.canAnswer);
            }

            // Se agrega a los reemplazamientos, la hora a la que se lleva a cabo la conversacion
            replacements.Clear();
            replacements["time"] = LevelManager.Instance.GetCurrentFormattedHour();

            // Tambien, si el jugador pregunta por otro personaje, se agrega el tipo de relacion
            // del personaje que estaba hablado con este primero
            List<string> names = personality.GetNameMatches(query);
            if (names != null && names.Count > 0)
            {
                string summary = personality.GetRelationshipSummary(names);
                enableRag = false;
                replacements["context"] = summary;
            }

            // Se realiza la peticion
            _ = provider.ChatCompletion(query, dialogBox.SetText, dialogBox.EndSetText, onError, replacements, enableRag);
        }, onError);
    }

    private void BaseGenerateTextNode(TextNode nextNode, LLMProvider provider, Personality personality, DialogBox dialogBox)
    {
        // Existen dos formas posibles
        dialogBox.WaitForText(personality.DisplayName);

        if (provider.LLMLocation == LLMLocation.REMOTE_SERVER && useTools)
        {
            // Se realiza mediante tools (solo en remoto)
            BaseGenerateNextNodeWithTools(nextNode, provider, dialogBox);
        }
        else
        {
            // Se realiza mediante una peticion previa
            BaseGenerateNextNodeWithoutTools(nextNode, provider, personality, dialogBox);
        }
    }

    private TextNode GenerateTextNode(LLMProvider provider, Personality personality, DialogBox dialogBox)
    {
        TextNode nextNode = CreateInstance<TextNode>();
        nextNode.characterName = personality.DisplayName;
        nextNode.canAnswer = true;

        BaseGenerateTextNode(nextNode, provider, personality, dialogBox);

        return nextNode;
    }

    // Se pide al modelo que genere un json con las pregutnas y respuestas en base a la conversacion hasta el momento 
    private DialogNode GenerateQuestion(LLMProvider provider, Personality personality, DialogBox dialogBox)
    {
        const int MIN_ANSWERS = 2;
        const int MAX_ANSWERS = 4;

        RelationshipsManager relationshipsManager = RelationshipsManager.Instance;

        // Hay que indicar en el mensaje el formato del json
        string systemMessage = $@"{personality.RoleSummary}.
Se te proporcionará el historial reciente reciente de la conversación con el jugador.
A partir de este contexto, tu tarea es formular una pregunta coherente y relevante que conecte directamente con lo último que ha dicho el jugador.
Tu prioridad es que la pregunta fluya de forma natural dentro de la conversacion, aunque no esté relacionada contigo como personaje.
Evita forzar un giro o introducir temas nuevos que no correspondan al momento actual.
Acompaña la pregunta con varias opciones de respuesta que el jugador pueda elegir. Estas opciones deben ofrecer distintos enfoques, emociones o decisiones posibles.

La respuesta debe estar estructurada según el siguiente JSON Schema, que define cómo debes presentar la pregunta y las posibles repuestas:
{{
    ""type"": ""object"",
    ""properties"": {{
        ""question"": {{
            ""type"": ""string"",
            ""description"": ""La pregunta que formula {personality.DisplayName} dentro de la conversacion.""
        }},
        ""answers"": {{
            ""type"": ""array"",
            ""description"": ""Las posibles respuestas que el jugador puede elegir."",
            ""items"" {{
                ""answer"": {{
                    ""type"": ""object"",
                    ""properties"": {{
                        ""text"": {{
                            ""type"": ""string"",
                            ""description"": ""Texto de una posible respuesta del jugador. Debe ser breve.""
                        }},
                        ""friendship_points"": {{
                            ""type"": ""integer"",
                            ""descripcion"": ""Nivel de felicidad con el que incrementa la relación entre {personality.DisplayName} y el jugador."",
                            ""minimum"": {relationshipsManager.QuestionMinPoints},
                            ""maximum"": {relationshipsManager.QuestionMaxPoints}
                        }}
                    }},
                    ""required"": [""text"", ""happiness""]
                }}
            }}
        }},
        ""minItems"": {MIN_ANSWERS},
        ""maxItems"": {MAX_ANSWERS}
    }},
    ""required"": [""question"", ""answers""]
}}";

        // Para obtener una pregunta que hile con la conversacion actual, se le proporciona toda la conversacion hasta el momento
        ChatBufferMemory chatMemory = provider.ChatMemory.Clone() as ChatBufferMemory;
        chatMemory.SetSystemMessage(systemMessage);

        chatMemory.AddUserMessage($@"Esta ha sido la ultima frase del jugador: {query}.
Formula una pregunta dirigida al jugador, junto con las posibles respuestas que podria elegir.");

        // Se crea la cadena de nodos que viene a continuacion

        // El siguiente nodo es uno de texto, que muestra la pregunta
        TextNode nextNode = CreateInstance<TextNode>();
        nextNode.characterName = personality.DisplayName;
        nextNode.canAnswer = false;

        const char DOT_CHAR = '.';

        // Si hay algun error (por ejemplo, no se ha podido parsear el json) se trata de generar texto regular
        Action<string> onError = error =>
        {
            nextNode.canAnswer = true;
            dialogBox.SetCanAnswer(nextNode.canAnswer);
            BaseGenerateTextNode(nextNode, provider, personality, dialogBox);
        };

        Action<NPCQuestion> onComplete = question =>
        {
            if (!string.IsNullOrEmpty(question.Question))
            {
                provider.ChatMemory.AddUserMessage(query);
                provider.ChatMemory.AddAssistantMessage(question.Question);

                // A continuacion del nodo de la pregunta, va un nodo de opcion con las diferentes opciones
                ChoiceNode choiceNode = CreateInstance<ChoiceNode>();
                nextNode.AddNextNode(choiceNode);

                foreach (PlayerAnswer playerAnswer in question.Answers)
                {
                    string text = playerAnswer.Text.EndsWith(DOT_CHAR) ? playerAnswer.Text : playerAnswer.Text + DOT_CHAR;
                    choiceNode.choices.Add(text);

                    int friendShipPoints = Math.Clamp(playerAnswer.FriendshipPoints, relationshipsManager.QuestionMinPoints, relationshipsManager.QuestionMaxPoints);
                    UnityEvent action = new UnityEvent();
                    action.AddListener(() =>
                    {
                        // Ademas, cada opcion reporta diferentes puntos de amistad
                        RelationshipsManager.Instance.UpdateFriendship(personality.DisplayName, friendShipPoints);
                    });
                    choiceNode.actions.Add(action);

                    // Luego de cada opcion, continua la conversacion generando nodos
                    LLMNode llmNode = CreateInstance<LLMNode>();
                    llmNode.query = text;
                    llmNode.useTools = false;
                    // Despues de una pregutna, no puede ir otra pregutna
                    llmNode.avoidQuestions = true;
                    choiceNode.AddNextNode(llmNode);
                }
                //dialogBox.SetText(question.Question);
                //dialogBox.EndSetText(question.Question);
                dialogBox.SetDialog(question.Question);
            }
            else
            {
                onError("No se ha generado la pregunta correctamente");
            }
        };

        _ = provider.JsonCompletion(chatMemory, onComplete, onError);

        return nextNode;
    }

    public DialogNode GenerateNextNode(LLMProvider provider, Personality personality, DialogBox dialogBox)
    {
        DialogNode node = null;

        float randomValue = UnityEngine.Random.value;
        float questionProb = questionProbability / 100.0f;
        bool canGenerateQuestion = randomValue < questionProb;

        // Se puede generar nodos de dos tipos
        // Nota: aunque se puede hacer con modelos en local esta desactivado porque generalmetne no son capaces de llevarlo a cabo

        if (!avoidQuestions && canGenerateQuestion && provider.LLMLocation == LLMLocation.REMOTE_SERVER)
        {
            // Nodos de pregunta
            node = GenerateQuestion(provider, personality, dialogBox);
        }
        else
        {
            // Nodos de texto regular
            node = GenerateTextNode(provider, personality, dialogBox);
        }
        return node;
    }
}
