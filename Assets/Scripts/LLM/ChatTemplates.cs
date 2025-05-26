using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;
using Utilities;

namespace LLM
{
    public enum ChatTemplateType
    {
        [Description("Chat ML")]
        CHAT_ML,

        [Description("LLama2 Chat")]
        LLAMA_2_CHAT,

        [Description("Llama3 Instruct")]
        LLAMA_3_INSTRUCT,

        [Description("Mistral Instruct")]
        MISTRAL_INSTRUCT,

        [Description("Gemma")]
        GEMMA,

        [Description("Alpaca")]
        ALPACA,

        [Description("Vicuna")]
        VICUNA,

        [Description("Amber Chat")]
        AMBER_CHAT
    }

    // Clase abstracta que define una plantilla de chat
    // El modelo solo es capaz de completar texto, por lo tanto, para mantener una conversacion
    // hay que convertir los mensajes en texto plano de acuerdo a una plantilla, que depende del tipo de modelo
    public abstract class ChatTemplate
    {
        private static Dictionary<ChatTemplateType, ChatTemplate> chatTemplates;
        private static Dictionary<string, ChatTemplateType> nameMatches;
        private static Dictionary<string, ChatTemplateType> templateMatches;

        protected StringBuilder stringBuilder = new StringBuilder();

        static ChatTemplate()
        {
            List<ChatTemplate> chatTemplateList = new List<ChatTemplate>()
            {
                new ChatMLTemplate(),
                new LLama2ChatTemplate(),
                new Llama3InstructTemplate(),
                new MistralInstructTemplate(),
                new GemmaTemplate(),
                new AlpacaTemplate(),
                new VicunaTemplate(),
                new AmberChatTemplate()
            };

            chatTemplates = new Dictionary<ChatTemplateType, ChatTemplate>();
            nameMatches = new Dictionary<string, ChatTemplateType>();
            templateMatches = new Dictionary<string, ChatTemplateType>();

            foreach (ChatTemplate template in chatTemplateList)
            {
                ChatTemplateType templateId = template.GetId();
                string templateName = template.GetId().GetDescriptionCached();
                chatTemplates[templateId] = template;

                foreach (string nameMatch in template.GetNameMatches())
                {
                    nameMatches[nameMatch] = template.GetId();
                }
                foreach (string templateMatch in template.GetTemplateMatches())
                {
                    templateMatches[templateMatch] = template.GetId();
                }
            }
        }

        // Se obtiene la plantilla directamente
        public static ChatTemplate GetTemplate(ChatTemplateType type)
        {
            return chatTemplates[type];
        }

        // Se trata de obtener la plantilla a partir de una plantilla definida en Jinja
        // En python, estas plantilla suelen estar definidas en un lenguaje llamado Jinja,
        // que permite definir plantillas de string facilmente
        // En el modelo, suele venir almacenado dicha plantilla
        public static ChatTemplate GetTemplateFromJinja(string jinjaTemplate)
        {
            string templateTrim = jinjaTemplate.Trim();
            if (templateMatches.TryGetValue(templateTrim, out ChatTemplateType chatTemplateType))
            {
                return GetTemplate(chatTemplateType);
            }
            return null;
        }

        // Se trata de obtener la plantilla a partir de un nombre con el corresponda
        // Luego, se prueba tanto con el nombre que viene almacenado en el modelo, como el nombre del propio archivo
        public static ChatTemplate GetTemplateFromName(string name)
        {
            string nameLower = name.ToLower();
            foreach (var pair in nameMatches)
            {
                if (nameLower.Contains(pair.Key))
                {
                    return GetTemplate(pair.Value);
                }
            }
            return null;
        }

        // Se trata de obtener la plantilla leyendo a partir de la informacion que tiene el propio modelo
        // Un modelo .gguf suele contener la plantilla Jinja que utiliza y su nombre
        public static ChatTemplate GetTemplateFromGGUF(LlamaCpp.GGUFReader ggufReader)
        {
            // Plantilla Jinja
            string jinjaTemplate = ggufReader.GetTemplateChat();
            ChatTemplate chatTemplate = GetTemplateFromJinja(jinjaTemplate);
            if (chatTemplate != null)
            {
                return chatTemplate;
            }

            // Nombre del modelo
            string modelName = ggufReader.GetModelName();
            chatTemplate = GetTemplateFromName(modelName);
            if (chatTemplate != null)
            {
                return chatTemplate;
            }


            // Nombre del archivo
            string filename = Path.GetFileNameWithoutExtension(ggufReader.ModelPath);
            chatTemplate = GetTemplateFromName(filename);
            if (chatTemplate != null)
            {
                return chatTemplate;
            }

            return null;
        }

        // Se convierten los mensajes en un texto plano
        // addGenerationRole --> agregar al final, el comienzo del proximo mensaje del sistema, induciendole para que continue la conversacion
        // addSpecial --> agregar el BOS token al principio de la conversacion
        public virtual string FormatPrompt(string systemMessage, List<ChatMessage> chatMessages, string systemRole, string userRole, string assistantRole, bool addGenerationPrompt = true, bool addSpecial = false)
        {
            stringBuilder.Clear();
            if (addSpecial)
            {
                stringBuilder.Append(GetPromptPrefix());
            }

            if (!string.IsNullOrEmpty(systemMessage))
            {
                stringBuilder.Append(GetRequestPrefix() + GetSystemPrefix(systemRole) + systemMessage.Trim() + GetSystemSuffix());
            }

            for (int i = 0; i < chatMessages.Count; i += 2)
            {
                if (i > 0)
                {
                    stringBuilder.Append(GetRequestPrefix());
                }
                stringBuilder.Append(GetUserPrefix(chatMessages[i].Role) + chatMessages[i].Content.Trim() + GetUserSuffix());
                if (i + 1 < chatMessages.Count)
                {
                    stringBuilder.Append(GetAssistantPrefix(chatMessages[i + 1].Role) + chatMessages[i + 1].Content.Trim() + GetAssistantSuffix());
                }
            }

            if (addGenerationPrompt && GenerationPromptSupported())
            {
                stringBuilder.Append(GetAssistantPrefix(assistantRole));
            }

            return stringBuilder.ToString();
        }

        protected List<string> AddStopWordsWithLineBreaks(List<string> stopWords)
        {
            List<string> newStopWords = new List<string>();
            foreach (string stopWord in stopWords)
            {
                newStopWords.Add(stopWord);
                newStopWords.Add($"\n{stopWord}");
            }
            return newStopWords;
        }

        protected abstract ChatTemplateType GetId();
        // Nombres a traves de los cuales determinar la plantilla
        protected virtual List<string> GetNameMatches() { return new List<string>(); }
        // Plantillas Jinja a traves de las cuales determinar la plantilla
        protected virtual List<string> GetTemplateMatches() { return new List<string>(); }
        // Token especial que indica el comienzo de una secuencia
        protected virtual string GetBOSToken() { return ""; }
        // Token especial que indica el final de una secuencia
        protected virtual string GetEOSToken() { return ""; }
        protected virtual string GetPromptPrefix() { return ""; }
        protected virtual string GetRequestPrefix() { return ""; }
        protected virtual string GetSystemPrefix(string role) { return ""; }
        protected virtual string GetSystemSuffix() { return ""; }
        protected virtual string GetUserPrefix(string role) { return ""; }
        protected virtual string GetUserSuffix() { return ""; }
        protected virtual string GetAssistantPrefix(string role) { return ""; }
        protected virtual string GetAssistantSuffix() { return ""; }
        protected virtual bool GenerationPromptSupported() { return true; }
        // Son palabras que indican al modelo que pare de generar
        // Si se incluye por defecto el EOS token, los resultados del modelo son mejores
        public virtual List<string> GetStopWords()
        {
            List<string> stopWords = new List<string>();
            if (GetEOSToken() != "")
            {
                stopWords.Add(GetEOSToken());
                stopWords = AddStopWordsWithLineBreaks(stopWords);
            }
            return stopWords;
        }
    }
    // https://github.com/jndiogo/LLM-chat-templates?tab=readme-ov-file#chatml
    // https://github.com/chujiezheng/chat_templates/blob/main/chat_templates/chatml.jinja
    public class ChatMLTemplate : ChatTemplate
    {
        protected override ChatTemplateType GetId() { return ChatTemplateType.CHAT_ML; }
        protected override List<string> GetNameMatches() { return new List<string>() { "chatml", "hermes", "qwen", "yi-chat", "orca" }; }
        protected override List<string> GetTemplateMatches() { return new List<string>() { "{% for message in messages %}{{'<|im_start|>' + message['role'] + '\n' + message['content'] + '<|im_end|>' + '\n'}}{% endfor %}{% if add_generation_prompt %}{{ '<|im_start|>assistant\n' }}{% endif %}" }; }
        protected override string GetSystemPrefix(string role) { return $"<|im_start|>{role}\n"; }
        protected override string GetSystemSuffix() { return "<|im_end|>\n"; }
        protected override string GetUserPrefix(string role) { return $"<|im_start|>{role}\n"; }
        protected override string GetUserSuffix() { return "<|im_end|>\n"; }
        protected override string GetAssistantPrefix(string role) { return $"<|im_start|>{role}\n"; }
        protected override string GetAssistantSuffix() { return "<|im_end|>\n"; }
    }

    // https://github.com/jndiogo/LLM-chat-templates?tab=readme-ov-file#llama-2
    // https://github.com/chujiezheng/chat_templates/blob/main/chat_templates/llama-2-chat.jinja
    // BOS y EOS --> https://huggingface.co/meta-llama/Llama-2-7b-chat-hf/blob/main/tokenizer_config.json#L12
    public class LLama2ChatTemplate : ChatTemplate
    {
        protected override ChatTemplateType GetId() { return ChatTemplateType.LLAMA_2_CHAT; }
        protected override List<string> GetNameMatches() { return new List<string>() { "llama-2", "llama v2" }; }
        protected override List<string> GetTemplateMatches() { return new List<string>() { "{% if messages[0]['role'] == 'system' %}{% set loop_messages = messages[1:] %}{% set system_message = messages[0]['content'] %}{% else %}{% set loop_messages = messages %}{% set system_message = false %}{% endif %}{% for message in loop_messages %}{% if (message['role'] == 'user') != (loop.index0 % 2 == 0) %}{{ raise_exception('Conversation roles must alternate user/assistant/user/assistant/...') }}{% endif %}{% if loop.index0 == 0 and system_message != false %}{% set content = '<<SYS>>\\n' + system_message + '\\n<</SYS>>\\n\\n' + message['content'] %}{% else %}{% set content = message['content'] %}{% endif %}{% if message['role'] == 'user' %}{{ bos_token + '[INST] ' + content.strip() + ' [/INST]' }}{% elif message['role'] == 'assistant' %}{{ ' '  + content.strip() + ' ' + eos_token }}{% endif %}{% endfor %}" }; }
        protected override string GetBOSToken() { return "<s>"; }
        protected override string GetEOSToken() { return "</s>"; }
        protected override string GetPromptPrefix() { return GetBOSToken(); }
        protected override string GetRequestPrefix() { return "[INST] "; }
        protected override string GetSystemPrefix(string role) { return "<<SYS>>\n"; }
        protected override string GetSystemSuffix() { return "\n<</SYS>>\n\n"; }
        protected override string GetUserSuffix() { return " [/INST]"; }
        protected override string GetAssistantPrefix(string role) { return " "; }
        protected override string GetAssistantSuffix() { return " " + GetEOSToken(); }
        protected override bool GenerationPromptSupported() { return false; }
    }

    // https://github.com/jndiogo/LLM-chat-templates?tab=readme-ov-file#llama-3
    // https://github.com/chujiezheng/chat_templates/blob/main/chat_templates/llama-3-instruct.jinja
    // https://www.llama.com/docs/model-cards-and-prompt-formats/meta-llama-3/
    // BOS y EOS --> https://huggingface.co/DLBDAlkemy/Meta-Llama-3-8B_continual_kb_all_chunks_AMPLIFON_systemPromptNone_15_v0/blob/bb2f1520a911501892027ab702c9b3c1a37a4834/tokenizer_config.json
    public class Llama3InstructTemplate : ChatTemplate
    {
        protected override ChatTemplateType GetId() { return ChatTemplateType.LLAMA_3_INSTRUCT; }
        protected override List<string> GetNameMatches() { return new List<string>() { "llama-3", "llama v3" }; }
        protected override List<string> GetTemplateMatches() { return new List<string>() { "{% if not add_generation_prompt is defined %}{% set add_generation_prompt = false %}{% endif %}{% set loop_messages = messages %}{% for message in loop_messages %}{% set content = '<|start_header_id|>' + message['role'] + '<|end_header_id|>\n\n'+ message['content'] | trim + '<|eot_id|>' %}{% if loop.index0 == 0 %}{% set content = bos_token + content %}{% endif %}{{ content }}{% endfor %}{% if add_generation_prompt %}{{ '<|start_header_id|>assistant<|end_header_id|>\n\n' }}{% else %}{{ eos_token }}{% endif %}" }; }
        protected override string GetBOSToken() { return "<|begin_of_text|>"; }
        protected override string GetEOSToken() { return "<|end_of_text|>"; }
        protected override string GetPromptPrefix() { return GetBOSToken(); }
        protected override string GetSystemPrefix(string role) { return $"<|start_header_id|>{role}<|end_header_id|>\n\n"; }
        protected override string GetSystemSuffix() { return "<|eot_id|>"; }
        protected override string GetUserPrefix(string role) { return $"<|start_header_id|>{role}<|end_header_id|>\n\n"; }
        protected override string GetUserSuffix() { return "<|eot_id|>"; }
        protected override string GetAssistantPrefix(string role) { return $"<|start_header_id|>{role}<|end_header_id|>\n\n"; }
        protected override string GetAssistantSuffix() { return "<|eot_id|>"; }
        public override List<string> GetStopWords() { return AddStopWordsWithLineBreaks(new List<string>() { GetEOSToken(), "<|eot_id|>" }); }
    }


    // https://github.com/jndiogo/LLM-chat-templates?tab=readme-ov-file#mixtral
    // https://github.com/jndiogo/LLM-chat-templates?tab=readme-ov-file#mixtral
    // https://github.com/chujiezheng/chat_templates/blob/main/chat_templates/mistral-instruct.jinja
    // BOS y EOS --> https://huggingface.co/intfloat/e5-mistral-7b-instruct/blob/main/tokenizer_config.json
    public class MistralInstructTemplate : ChatTemplate
    {
        protected override ChatTemplateType GetId() { return ChatTemplateType.MISTRAL_INSTRUCT; }
        protected override List<string> GetNameMatches() { return new List<string>() { "mistral", "mixtral" }; }
        protected override List<string> GetTemplateMatches() { return new List<string>() { "{% if messages[0]['role'] == 'system' %}{% set system_message = messages[0]['content'] | trim + '\n\n' %}{% set messages = messages[1:] %}{% else %}{% set system_message = '' %}{% endif %}{{ bos_token + system_message}}{% for message in messages %}{% if (message['role'] == 'user') != (loop.index0 % 2 == 0) %}{{ raise_exception('Conversation roles must alternate user/assistant/user/assistant/...') }}{% endif %}{% if message['role'] == 'user' %}{{ '[INST] ' + message['content'] | trim + ' [/INST]' }}{% elif message['role'] == 'assistant' %}{{ ' ' + message['content'] | trim + eos_token }}{% endif %}{% endfor %}" }; }
        protected override string GetBOSToken() { return "<s>"; }
        protected override string GetEOSToken() { return "</s>"; }
        protected override string GetPromptPrefix() { return GetBOSToken(); }
        protected override string GetRequestPrefix() { return "[INST] "; }
        protected override string GetSystemSuffix() { return "\n\n"; }
        protected override string GetUserSuffix() { return " [/INST]"; }
        protected override string GetAssistantSuffix() { return GetEOSToken(); }
        protected override bool GenerationPromptSupported() { return false; }
    }

    // https://github.com/jndiogo/LLM-chat-templates?tab=readme-ov-file#gemma
    // https://github.com/chujiezheng/chat_templates/blob/main/chat_templates/gemma-it.jinja
    public class GemmaTemplate : ChatTemplate
    {
        protected override ChatTemplateType GetId() { return ChatTemplateType.GEMMA; }
        protected override List<string> GetNameMatches() { return new List<string>() { "gemma" }; }
        protected override List<string> GetTemplateMatches() { return new List<string>() { "{% if messages[0]['role'] == 'system' %}{% set system_message = messages[0]['content'] | trim + '\n\n' %}{% set messages = messages[1:] %}{% else %}{% set system_message = '' %}{% endif %}{% for message in messages %}{% if (message['role'] == 'user') != (loop.index0 % 2 == 0) %}{{ raise_exception('Conversation roles must alternate user/assistant/user/assistant/...') }}{% endif %}{% if loop.index0 == 0 %}{% set content = system_message + message['content'] %}{% else %}{% set content = message['content'] %}{% endif %}{% if (message['role'] == 'assistant') %}{% set role = 'model' %}{% else %}{% set role = message['role'] %}{% endif %}{{ '<start_of_turn>' + role + '\n' + content | trim + '<end_of_turn>\n' }}{% endfor %}{% if add_generation_prompt %}{{'<start_of_turn>model\n'}}{% endif %}" }; }
        protected override string GetSystemSuffix() { return "\n\n"; }
        protected override string GetUserPrefix(string role) { return $"<start_of_turn>{role}\n"; }
        protected override string GetUserSuffix() { return "<end_of_turn>\n"; }
        protected override string GetAssistantPrefix(string role)
        {
            if (role == "assistant")
            {
                role = "model";
            }
            return $"<start_of_turn>{role}\n";
        }
        protected override string GetAssistantSuffix() { return "<end_of_turn>\n"; }

        public override string FormatPrompt(string systemMessage, List<ChatMessage> messages, string systemRole, string humanRole, string AIRole, bool addGenerationPrompt = true, bool addSpecial = false)
        {
            List<ChatMessage> newMessages = new List<ChatMessage>(messages);
            // No tiene un apartado en concreto para el mensaje del sistema, por lo tanto,
            // lo que se indica en la documentacion es agregarlo al principo del primer mensaje de usuario
            if (string.IsNullOrEmpty(systemMessage))
            {
                if (messages.Count > 0)
                {
                    newMessages[0].Content = $"{systemMessage}\n\n{newMessages[0].Content}";
                }
            }
            return FormatPrompt(null, newMessages, systemRole, humanRole, AIRole, addGenerationPrompt, addSpecial);
        }
    }

    // https://github.com/jndiogo/LLM-chat-templates?tab=readme-ov-file#alpaca
    // https://github.com/chujiezheng/chat_templates/blob/main/chat_templates/alpaca.jinja
    // https://github.com/tatsu-lab/stanford_alpaca
    // BOS y EOS --> https://huggingface.co/allenai/open-instruct-stanford-alpaca-7b/blob/main/special_tokens_map.json
    public class AlpacaTemplate : ChatTemplate
    {
        protected override ChatTemplateType GetId() { return ChatTemplateType.ALPACA; }
        protected override List<string> GetNameMatches() { return new List<string>() { "alpaca" }; }
        protected override List<string> GetTemplateMatches() { return new List<string>() { "{% if messages[0]['role'] == 'system' %}{% set system_message = messages[0]['content'] | trim + '\n\n' %}{% set messages = messages[1:] %}{% else %}{% set system_message = '' %}{% endif %}{{ bos_token + system_message }}{% for message in messages %}{% if (message['role'] == 'user') != (loop.index0 % 2 == 0) %}{{ raise_exception('Conversation roles must alternate user/assistant/user/assistant/...') }}{% endif %}{% if message['role'] == 'user' %}{{ '### Instruction:\n' + message['content'] | trim + '\n\n' }}{% elif message['role'] == 'assistant' %}{{ '### Response:\n' + message['content'] | trim + eos_token + '\n\n' }}{% endif %}{% endfor %}{% if add_generation_prompt %}{{ '### Response:\n' }}{% endif %}" }; }
        protected override string GetBOSToken() { return "<s>"; }
        protected override string GetEOSToken() { return "</s>"; }
        protected override string GetPromptPrefix() { return GetBOSToken(); }
        protected override string GetSystemSuffix() { return "\n\n"; }
        protected override string GetUserPrefix(string role) { return "### Instruction:\n"; }
        protected override string GetUserSuffix() { return "\n\n"; }
        protected override string GetAssistantPrefix(string role) { return "### Response:\n"; }
        protected override string GetAssistantSuffix() { return GetEOSToken() + "\n\n"; }
    }

    // https://github.com/jndiogo/LLM-chat-templates?tab=readme-ov-file#vicuna
    // https://github.com/chujiezheng/chat_templates/blob/main/chat_templates/vicuna.jinja
    // https://github.com/lm-sys/FastChat/blob/main/docs/vicuna_weights_version.md#prompt-template
    // https://lmsys.org/blog/2023-03-30-vicuna/
    // BOS y EOS --> https://huggingface.co/lmsys/vicuna-7b-v1.5/blob/main/tokenizer_config.json
    public class VicunaTemplate : ChatTemplate
    {
        protected override ChatTemplateType GetId() { return ChatTemplateType.VICUNA; }
        protected override List<string> GetNameMatches() { return new List<string>() { "vicuna" }; }
        protected override List<string> GetTemplateMatches() { return new List<string>() { "{% if not add_generation_prompt is defined %}{% set add_generation_prompt = false %}{% endif %}{% for message in messages %}{% if message['role'] == 'system' %}{{message['content'] + ' '}}{% elif message['role'] == 'user' %}{{ 'USER: ' + message['content'] + ' '}}{% elif message['role'] == 'assistant' %}{{ 'ASSISTANT: ' + message['content'] + ' '}}{% endif %}{% endfor %}{% if add_generation_prompt %}{{ 'ASSISTANT: '}}{% endif %}" }; }
        protected override string GetBOSToken() { return "<s>"; }
        protected override string GetEOSToken() { return "</s>"; }
        protected override string GetPromptPrefix() { return GetBOSToken(); }
        protected override string GetSystemSuffix() { return "\n\n"; }
        protected override string GetUserPrefix(string role) { return "USER: "; }
        protected override string GetUserSuffix() { return "\n"; }
        protected override string GetAssistantPrefix(string role) { return "ASSISTANT: "; }
        protected override string GetAssistantSuffix() { return GetEOSToken() + "\n"; }
    }

    // https://github.com/jndiogo/LLM-chat-templates?tab=readme-ov-file#amberchat
    // https://github.com/chujiezheng/chat_templates/blob/main/chat_templates/amberchat.jinja
    // https://huggingface.co/TheBloke/AmberChat-GGUF
    // BOS y EOS --> https://huggingface.co/LLM360/AmberChat/blob/main/tokenizer_config.json
    public class AmberChatTemplate : ChatTemplate
    {
        protected override ChatTemplateType GetId() { return ChatTemplateType.AMBER_CHAT; }
        protected override List<string> GetNameMatches() { return new List<string>() { "amberchat" }; }
        protected override List<string> GetTemplateMatches() { return new List<string>() { "{% if messages[0]['role'] == 'system' %}{% set system_message = messages[0]['content'] | trim + '\n' %}{% set messages = messages[1:] %}{% else %}{% set system_message = '' %}{% endif %}{{ bos_token + system_message }}{% for message in messages %}{% if (message['role'] == 'user') != (loop.index0 % 2 == 0) %}{{ raise_exception('Conversation roles must alternate user/assistant/user/assistant/...') }}{% endif %}{% if message['role'] == 'user' %}{{ '###Human: ' + message['content'] | trim + '\n' }}{% elif message['role'] == 'assistant' %}{{ '###Assistant: ' + message['content'] | trim + '\n' }}{% endif %}{% endfor %}{% if add_generation_prompt %}{{ '###Assistant:' }}{% endif %}" }; }
        protected override string GetBOSToken() { return "<s>"; }
        protected override string GetEOSToken() { return "</s>"; }
        protected override string GetPromptPrefix() { return GetBOSToken(); }
        protected override string GetSystemSuffix() { return "\n"; }
        protected override string GetUserPrefix(string role) { return "### Human: "; }
        protected override string GetUserSuffix() { return "\n"; }
        protected override string GetAssistantPrefix(string role) { return "### Assistant: "; }
        protected override string GetAssistantSuffix() { return "\n"; }
    }
}
