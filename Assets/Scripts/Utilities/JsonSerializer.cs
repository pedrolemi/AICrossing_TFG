using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Text.RegularExpressions;

namespace Utilities
{
    // Clase que permtie serializar y deserializar un texto siguiendo unos ajustos determinados
    public static class JsonSerializer
    {
        // Se usa la siguiente expresion regular para convertir de CamelCase a camel_case, que esta ultima es la convencion en json
        private static readonly Regex regExpression = new Regex("[A-Z]", RegexOptions.Compiled);

        private static readonly MatchEvaluator matchEvaluator = match =>
        {
            string lowerMatch = match.Value.ToLowerInvariant();
            return match.Index > 0 ? $"_{lowerMatch}" : lowerMatch;
        };

        public class CustomNamingStrategy : NamingStrategy
        {
            protected override string ResolvePropertyName(string name)
            {
                return regExpression.Replace(name, matchEvaluator);
            }
        }

        private static readonly JsonSerializerSettings serializerSettings = new JsonSerializerSettings
        {
            // No se incluyen atributos con valores null
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new DefaultContractResolver()
            {
                NamingStrategy = new CustomNamingStrategy()
            }
        };

        public static string Serialize<T>(T jsonObject)
        {
            return JsonConvert.SerializeObject(jsonObject, serializerSettings);
        }

        public static bool TryDeserialize<T>(string json, out T result) where T : new()
        {
            try
            {
                result = JsonConvert.DeserializeObject<T>(json);
                return !string.IsNullOrEmpty(json);
            }
            catch
            {
                result = new T();
                return false;
            }
        }

        public static T Deserialize<T>(string json)
        {
            return JsonConvert.DeserializeObject<T>(json, serializerSettings);
        }
    }
}