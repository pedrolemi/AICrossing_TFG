using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace LLM
{
    [Serializable]
    public class ChatMessage
    {
        public string Role { get; set; }
        public string Content { get; set; }
    }

    // Clase que implementa una memoria para un modelo de lenguaje
    // Almacena los mensajes que se usan en la conversacion
    public class ChatBufferMemory : ICloneable
    {
        // Roles que pueden tomar los mensajes
        protected const string SYSTEM_ROLE = "system";
        protected const string USER_ROLE = "user";
        protected const string ASSISTANT_ROLE = "assistant";

        // Mensaje del sistema, que define la persona del modelo de lenguaje
        // Solo hay uno al principio del todo
        protected string systemMessage;
        // Resto de mensajes
        protected List<ChatMessage> messages;

        public ChatBufferMemory()
        {
            messages = new List<ChatMessage>();
        }

        public ChatBufferMemory(string systemMessage)
        {
            messages = new List<ChatMessage>();
            SetSystemMessage(systemMessage);
        }

        public virtual object Clone()
        {
            return new ChatBufferMemory()
            {
                systemMessage = systemMessage,
                messages = new List<ChatMessage>(messages)
            };
        }

        public void ClearChat()
        {
            messages.Clear();
            AddMessage(new ChatMessage()
            {
                Role = SYSTEM_ROLE,
                Content = systemMessage
            });
        }

        public void SetSystemMessage(string newSystemMessage)
        {
            systemMessage = newSystemMessage;
        }

        // En la implementacion mas sencilla se mantienen todos los mensajes,
        // pero en implementaciones mas avanzadas se utilizan tecnicas para eliminar
        // los mensajes mas viejos y asi, no saturar el contexto del modelo de lenguaje
        protected virtual void ModifyChat(ChatMessage message) { }

        private void AddMessage(ChatMessage message)
        {
            // Es obligatorio que se intercalen mensajes del usuario y del asistente
            if (message.Role == USER_ROLE && messages.Count > 0)
            {
                ChatMessage lastMessage = messages.Last();
                if (lastMessage.Role == USER_ROLE)
                {
                    Debug.LogError("Cannot add a user message after another user message.");
                    return;
                }
            }

            if (message.Role == ASSISTANT_ROLE)
            {
                ChatMessage lastMessage = messages.LastOrDefault();
                if (lastMessage == null || lastMessage.Role != USER_ROLE)
                {
                    Debug.LogError("Assistant message must come after a user message.");
                    return;
                }
            }

            messages.Add(message);

            ModifyChat(message);
        }

        public void AddUserMessage(string content)
        {
            AddMessage(new ChatMessage()
            {
                Role = USER_ROLE,
                Content = content
            });
        }

        public void AddAssistantMessage(string content)
        {
            AddMessage(new ChatMessage()
            {
                Role = ASSISTANT_ROLE,
                Content = content
            });
        }

        public void RemoveLastMessage()
        {
            if (messages.Count > 0)
            {
                messages.RemoveAt(messages.Count - 1);
            }
        }

        // Convertir los mensajes en texto de acuerdo a una plantilla de chat
        public string FormatPrompt(ChatTemplate chatTemplate, bool addGenerationPrompt = true, bool addSpecial = false, Dictionary<string, string> replacements = null)
        {
            string prompt = chatTemplate.FormatPrompt(systemMessage, messages, SYSTEM_ROLE, USER_ROLE, ASSISTANT_ROLE, addGenerationPrompt, addSpecial);

            // Se realizan los reemplazamientos [...] necesarios
            if (replacements != null)
            {
                foreach (var replacement in replacements)
                {
                    string key = $"[{replacement.Key}]";
                    string value = replacement.Value;
                    prompt = prompt.Replace(key, value);
                }
            }
            return prompt;
        }

        // Convertir los mensajes en el formato que necesita Groq Cloud
        public List<Groq.Message> GetGroqMessages(Dictionary<string, string> replacements = null)
        {
            List<Groq.Message> groqMessages = new List<Groq.Message>()
            {
                new Groq.Message()
                {
                    Content = systemMessage,
                    Role = SYSTEM_ROLE
                }
            };
            foreach (ChatMessage message in messages)
            {
                groqMessages.Add(new Groq.Message()
                {
                    Content = message.Content,
                    Role = message.Role,
                });
            }

            // Se realizan los reemplazamientos [...] necesarios
            if (replacements != null)
            {
                foreach (var replacement in replacements)
                {
                    string key = $"[{replacement.Key}]";
                    string value = replacement.Value;
                    foreach (Groq.Message message in groqMessages)
                    {
                        string content = message.Content;
                        if (content.Contains(key))
                        {
                            message.Content = content.Replace(key, value);
                            break;
                        }
                    }
                }
            }
            return groqMessages;
        }

        public int Count()
        {
            return messages.Count;
        }
    }

    // Clase que implementa una memoria, en la que solo se mantiene un numero determinado de interacciones
    // De este modo, se mantiene fresco el contexto del modelo
    public class ChatBufferWindowMemory : ChatBufferMemory
    {
        private int messagesKeep;

        public ChatBufferWindowMemory(int interactionsKeep = 5)
        {
            messagesKeep = interactionsKeep * 2;
        }

        public ChatBufferWindowMemory(string systemMessage, int interactionsKeep = 5) : this(interactionsKeep)
        {
            SetSystemMessage(systemMessage);
        }

        public override object Clone()
        {
            return new ChatBufferWindowMemory()
            {
                systemMessage = systemMessage,
                messages = new List<ChatMessage>(messages),
                messagesKeep = messagesKeep
            };
        }

        protected override void ModifyChat(ChatMessage message)
        {
            // Se elimina una interaccion, es decir, de 2 en 2
            if (message.Role == ASSISTANT_ROLE)
            {
                // Cuando se alcanza el numero de interacciones determinado, se eliminan los primeros mensajes
                int interactionsRemove = Count() - messagesKeep;
                if (interactionsRemove > 0)
                {
                    messages.RemoveRange(0, interactionsRemove);
                }
            }
        }
    }
}