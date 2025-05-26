using LLM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEngine;

// Clase que define la personalidad de un personaje a traves de concatenar strings
public class Personality : MonoBehaviour
{
    private const int MIN_VALUE = 0;
    private const int MAX_VALUE = 4;

    private const string DESCRIPTION_HEAD = "Eres";
    private const char DESCRIPTION_TAIL = '.';

    private const string DISPLAY_NAME_HEAD = "Te llamas";
    private const string DISPLAY_NAME_MID = "y tienes la siguiente personalidad: ";
    private const string DISPLAY_NAME_TAIL = ".";

    private const string DESCRIPTION_SUM_MID = "es uno de los habitantes de la aldea. Es";

    private const string LIST_MID = ", ";
    private const string LIST_TAIL = ".";

    private const string CONVERSATION_PLACE = "La charla ocurre en una pequeñaa aldea ubicada en una isla tranquila, donde viven unos pocos habitantes. Son las [time].";
    private const string CONVERSATION_GOAL = "Estás conversando con alguien que acaba de mudarse al pueblo y aún no te conoce. El objetivo de la conversación es empezar a conocerse mejor.";

    private const string CONTEXT = @"El contexto para responder a la cuestión que te hace es:
[context].
Ten en cuenta que este contexto podría no ser adecuado. Por lo tanto, analiza si debes utilizarlo o no.";

    // Reglas que seguir durante la conversacion
    private const string RULES_HEAD = "Las reglas de estilo que debes seguir son: ";

    private static List<string> RULES = new List<string>() {
        "Proporciona únicamente diálogos, siempre desde la primera persona.",
        "Bajo ninguna circunstancia incluyas descripciones de tu estado, acciones o pensamientos.",
        "Los textos deben ser breves y adecuados para una conversación casual.",
        "Evita mencionar que eres una inteligencia artificial o un modelo de lenguaje."};

    [SerializeField]
    private string displayName = "Cortana";
    public string DisplayName => displayName;
    [SerializeField]
    private TextMeshProUGUI nameTag;
    [TextArea(5, 10)]
    [SerializeField]
    private string description = "un asistente personal";

    [SerializeField]
    [Range(MIN_VALUE, MAX_VALUE)]
    private int conscientiousness;
    public int Conscientiousness => conscientiousness;
    [SerializeField]
    [Range(MIN_VALUE, MAX_VALUE)]
    private int extraversion;
    public int Extraversion => extraversion;
    [SerializeField]
    [Range(MIN_VALUE, MAX_VALUE)]
    private int neuroticism;
    public int Neuroticism => neuroticism;
    [SerializeField]
    [Range(MIN_VALUE, MAX_VALUE)]
    private int openness;
    public int Openness => openness;
    [SerializeField]
    [Range(MIN_VALUE, MAX_VALUE)]
    private int agreeableness;
    public int Agreeableness => agreeableness;

    [SerializeField]
    [Range(MIN_VALUE, MAX_VALUE)]
    private int happiness;
    public int Happiness => happiness;
    [SerializeField]
    [Range(MIN_VALUE, MAX_VALUE)]
    private int anger;
    public int Anger => anger;
    [SerializeField]
    [Range(MIN_VALUE, MAX_VALUE)]
    private int sarcasm;
    public int Sarcasm => sarcasm;

    [SerializeField]
    [TextArea(5, 10)]
    private List<string> secrets;
    [SerializeField]
    [TextArea(5, 10)]
    private List<string> topics;

    private string role;
    public string RoleSummary { get; private set; }
    public string ThirdPersonRole { get; private set; }

    private StringBuilder stringBuilder;
    private PersonalityManager personalityManager;

    private Dictionary<string, string> relationships;

    void Start()
    {
        if (nameTag != null)
        {
            nameTag.text = displayName;
        }

        personalityManager = PersonalityManager.Instance;
        stringBuilder = new StringBuilder();

        role = GeneratyRole();
        RoleSummary = GenerateRoleSummary();
        ThirdPersonRole = GenerateThirdPersonRole();

        LLMProvider provider = GetComponent<LLMProvider>();
        if (provider)
        {
            if (provider.LLMLocation == LLMLocation.LOCAL)
            {
                provider.SetSystemMessage(RoleSummary);
            }
            else
            {
                provider.SetSystemMessage(role);
            }
        }

        relationships = personalityManager.GetRelationship(displayName);
    }

    public List<string> GetNameMatches(string text)
    {
        if (relationships != null)
        {
            text = text.ToLower();
            return relationships.Where(relation => text.Contains(relation.Key)).Select(relation => relation.Key).ToList();
        }
        return null;
    }

    public string GetRelationshipSummary(List<string> names)
    {
        const string RELATIONSHIP_HEAD = "Vuestra relación es";
        const string RELATIONSHIP_TAIL = ".";

        if (relationships != null)
        {
            string summary = "";
            for (int i = 0; i < names.Count(); ++i)
            {
                string name = names[i];
                if (relationships.ContainsKey(name))
                {
                    string type = relationships[name];
                    string role = PersonalityManager.Instance.GetPersonalityRole(name);
                    summary += $"{role} {RELATIONSHIP_HEAD} {type}{RELATIONSHIP_TAIL}";
                    if (i < summary.Count() - 1)
                    {
                        summary += "\n";
                    }
                }
            }
            return summary;
        }
        return null;
    }

    private string GeneratyRole()
    {
        // El rol esta formado por diferentes elementos
        // El segundo item es el numero de saltos de lineas que hay entre ellos
        List<(string, int)> roleDescription = new List<(string, int)>()
        {
            (GeneratePersonality(), 2),
            (GenerateTopicsDescription(), 1),
            (GenerateSecretsDescription(), 2),
            (CONVERSATION_PLACE, 2),
            (CONVERSATION_GOAL, 1),
            (CONTEXT, 2),
            (GenerateDialogeRules(), 0)
        };

        string result = "";
        foreach (var description in roleDescription)
        {
            string spacing = "";
            if (description.Item2 > 0)
            {
                spacing = new string('\n', description.Item2);
            }

            if (!string.IsNullOrEmpty(description.Item1))
            {
                result += $"{description.Item1}{spacing}";
            }
        }

        return result;
    }

    private string GeneratePersonality()
    {
        stringBuilder.Clear();
        // Nombre del personaje con breve trasnfondo
        stringBuilder.Append($"{DESCRIPTION_HEAD} {description}{DESCRIPTION_TAIL} {DISPLAY_NAME_HEAD} {displayName} {DISPLAY_NAME_MID}\n");
        // Se pide al personality manager que cree una descripcion a partir de las emociones y el estado de animo actual
        List<string> personalityDescription = personalityManager.GeneratePersonalityDescription(this);
        for (int i = 0; i < personalityDescription.Count; ++i)
        {
            string description = personalityDescription[i];
            if (!string.IsNullOrEmpty(description))
            {
                stringBuilder.Append($"- {description}");

                if (i < personalityDescription.Count - 1)
                {
                    stringBuilder.Append("\n");
                }
            }
        }
        return stringBuilder.ToString();
    }

    private string GenerateSecretsDescription()
    {
        const string SECRETS_HEAD = "Secretamente,";
        const string SECRETS_TAIL = "Evitarás cualquier mención acerca de estos temas.";

        if (secrets.Count > 0)
        {
            stringBuilder.Clear();
            string descriptions = string.Join(LIST_MID, secrets);
            stringBuilder.Append($"{SECRETS_HEAD}{descriptions}{LIST_TAIL} {SECRETS_TAIL}");
            return stringBuilder.ToString();
        }
        return null;
    }

    private string GenerateTopicsDescription()
    {
        const string TOPICS_HEAD = "Te gusta hablar sobre que";
        const string TOPICS_TAIL = "y lo mencionarás ocasionalmente en las conversaciones.";

        if (secrets.Count > 0)
        {
            stringBuilder.Clear();
            string descriptions = string.Join(LIST_MID, topics);
            stringBuilder.Append($"{TOPICS_HEAD} {descriptions} {TOPICS_TAIL}");
            return stringBuilder.ToString();
        }
        return null;
    }

    private string GenerateRoleSummary()
    {
        stringBuilder.Clear();
        stringBuilder.Append($"{DESCRIPTION_HEAD} {description}{DESCRIPTION_TAIL} {DISPLAY_NAME_HEAD} {displayName}{DISPLAY_NAME_TAIL}");
        return stringBuilder.ToString();
    }

    private string GenerateThirdPersonRole()
    {
        stringBuilder.Clear();
        stringBuilder.Append($"{displayName} {DESCRIPTION_SUM_MID} {description}{DESCRIPTION_TAIL}");
        return stringBuilder.ToString();
    }

    private string GenerateDialogeRules()
    {
        stringBuilder.Clear();
        if (RULES.Count > 0)
        {
            string rules = string.Join("\n", RULES.Select(rule => $"- {rule}"));
            stringBuilder.Append($"{RULES_HEAD}\n{rules}");
            return stringBuilder.ToString();
        }
        return null;
    }

    public void SetNameTagEnabled(bool enabled)
    {
        nameTag.enabled = enabled;
    }
}
