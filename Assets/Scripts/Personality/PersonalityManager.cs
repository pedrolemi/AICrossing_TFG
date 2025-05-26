using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEngine;

public class PersonalityManager : Singleton<PersonalityManager>
{
    private const string PERSONALITY_PATH = "Personality";

    private const char SPLIT = ':';
    private const string LIST_MID = ", ";
    private const string LIST_TAIL = ".";

    private const string OPEN_NAME = "openness";
    private const string CONSC_NAME = "conscientiousness";
    private const string EXT_NAME = "extraversion";
    private const string AGREE_NAME = "agreeableness";
    private const string NEURO_NAME = "neuroticism";

    private const string HAPPY_NAME = "happiness";
    private const string ANGER_NAME = "anger";
    private const string SARC_NAME = "sarcasm";

    private const string DEGREE_TERMS = "degree_terms";
    private const string ACTION_TERMS = "action_terms";
    private const string MOOD_TERMS = "mood_terms";

    private class Mood
    {
        public string name;
        public string adjective;
        public Mood(string name, string adjective)
        {
            this.name = name;
            this.adjective = adjective;
        }
    }

    private class Trait : Mood
    {
        private const string TRAIT_POSITIVE = "0";
        private const string TRAIT_NEGATIVE = "1";

        public List<string> features;
        public List<string> contradictions;

        public Trait(string name, string adjective) : base(name, adjective)
        {
            features = new List<string>();
            contradictions = new List<string>();

            string path = Path.Combine(PERSONALITY_PATH, "Traits", name);
            TextAsset textAsset = Resources.Load<TextAsset>(path);
            string[] lines = textAsset.text.Split('\n');
            foreach (string line in lines)
            {
                string[] splits = line.Split(SPLIT);
                if (splits[0] == TRAIT_POSITIVE)
                {
                    features.Add(splits[1].Trim());
                }
                else if (splits[0] == TRAIT_NEGATIVE)
                {
                    contradictions.Add(splits[1].Trim());
                }
            }
        }
    }

    private StringBuilder stringBuilder;

    private Dictionary<string, Trait> traits;
    private Dictionary<string, Mood> moods;

    private List<string> degreeTerms;
    private List<string> actionTerms;
    private List<string> moodTerms;

    private Dictionary<string, Personality> personalities;

    private Dictionary<string, Dictionary<string, string>> relationships;

    protected override void Awake()
    {
        base.Awake();
        stringBuilder = new StringBuilder();

        Trait openness = new Trait(OPEN_NAME, "amable");
        Trait conscientiousness = new Trait(CONSC_NAME, "responsable");
        Trait extraversion = new Trait(EXT_NAME, "extrovertido");
        Trait agreeableness = new Trait(AGREE_NAME, "neurótico");
        Trait neuroticsm = new Trait(NEURO_NAME, "sarcástico");
        List<Trait> traitsList = new List<Trait> { openness, conscientiousness, extraversion, agreeableness, neuroticsm };
        traits = new Dictionary<string, Trait>();
        foreach (Trait trait in traitsList)
        {
            traits[trait.name] = trait;
        }

        Mood happiness = new Mood(HAPPY_NAME, "feliz");
        Mood anger = new Mood(ANGER_NAME, "enfadado");
        Mood sarcasm = new Mood(SARC_NAME, "sarcástico");
        List<Mood> moodsList = new List<Mood>() { happiness, anger, sarcasm };
        moods = new Dictionary<string, Mood>();
        foreach (Mood mood in moodsList)
        {
            moods[mood.name] = mood;
        }

        degreeTerms = LoadTerms(DEGREE_TERMS);
        actionTerms = LoadTerms(ACTION_TERMS);
        moodTerms = LoadTerms(MOOD_TERMS);

        personalities = new Dictionary<string, Personality>();

        relationships = LoadRelationships();
    }

    // Se leen las relaciones del grafo definido en el fichero de texto
    private Dictionary<string, Dictionary<string, string>> LoadRelationships()
    {
        Dictionary<string, Dictionary<string, string>> relationships = new Dictionary<string, Dictionary<string, string>>();

        string path = Path.Combine(PERSONALITY_PATH, "relationships");
        TextAsset textAsset = Resources.Load<TextAsset>(path);

        string[] sections = textAsset.text.Split("\r\n\r\n");

        Dictionary<int, string> types = LoadRelationshipTypes(sections[0]);

        string[] relationshipsAux = sections[1].Split("\n");
        foreach (string relation in relationshipsAux)
        {
            string[] relationship = relation.Split();
            int value = Int32.Parse(relationship[2]);
            string type = types[value];

            string character1 = relationship[0].Trim().ToLower();
            string character2 = relationship[1].Trim().ToLower();

            AddRelationship(relationships, character1, character2, type);
            AddRelationship(relationships, character2, character1, type);
        }
        return relationships;
    }

    private Dictionary<int, string> LoadRelationshipTypes(string text)
    {
        string[] typesAux = text.Split('\n');

        Dictionary<int, string> types = new Dictionary<int, string>();
        foreach (string line in typesAux)
        {
            string[] values = line.Split(SPLIT);
            int value = Int32.Parse(values[0]);
            types[value] = values[1].Trim();
        }
        return types;
    }

    private void AddRelationship(Dictionary<string, Dictionary<string, string>> relationships,
        string character1, string character2, string type)
    {
        if (!relationships.ContainsKey(character1))
        {
            relationships[character1] = new Dictionary<string, string>();
        }
        relationships[character1].Add(character2, type);
    }

    public Dictionary<string, string> GetRelationship(string name)
    {
        name = name.ToLower();
        if (relationships.ContainsKey(name))
        {
            return relationships[name];
        }
        return null;
    }

    private List<string> LoadTerms(string fileName)
    {
        string path = Path.Combine(PERSONALITY_PATH, "Terms", fileName);
        TextAsset textAsset = Resources.Load<TextAsset>(path);
        return textAsset.text.Split('\n').Select(term => term.Trim()).ToList();
    }

    public List<string> GeneratePersonalityDescription(Personality personality)
    {
        List<string> description = new List<string>() {
            GenerateTraitDescription(traits[CONSC_NAME], personality.Conscientiousness),
            GenerateTraitDescription(traits[EXT_NAME], personality.Extraversion),
            GenerateTraitDescription(traits[NEURO_NAME], personality.Neuroticism),
            GenerateTraitDescription(traits[OPEN_NAME], personality.Openness),
            GenerateTraitDescription(traits[AGREE_NAME], personality.Agreeableness)
        };

        List<(string, int)> moods = new List<(string, int)>()
        {
            (HAPPY_NAME, personality.Happiness),
            (ANGER_NAME, personality.Anger),
            (SARC_NAME, personality.Sarcasm)
        };
        description.Add(GenerateMoodDescription(moods));

        personalities[personality.DisplayName.ToLower()] = personality;

        return description;
    }

    public string GetPersonalityRole(string name)
    {
        if (personalities.ContainsKey(name))
        {
            return personalities[name].ThirdPersonRole;
        }
        return null;
    }

    private string GenerateTraitDescription(Trait trait, int value)
    {
        const string ACTION_MID = "haces lo siguiente: ";

        string description = "";
        if (value > 0)
        {
            --value;
            stringBuilder.Clear();
            stringBuilder.Append($"{degreeTerms[value]} {trait.adjective}{LIST_TAIL} ");

            stringBuilder.Append($"{actionTerms[value]} {ACTION_MID}");
            for (int i = 0; i < trait.features.Count; ++i)
            {
                stringBuilder.Append(trait.features[i]);
                if (i < trait.features.Count() - 1)
                {
                    stringBuilder.Append(LIST_MID);
                }
                else
                {
                    stringBuilder.Append(LIST_TAIL);
                }
            }

            stringBuilder.Append($" {actionTerms[actionTerms.Count() - value - 1]} {ACTION_MID}");
            for (int i = 0; i < trait.contradictions.Count(); ++i)
            {
                stringBuilder.Append(trait.contradictions[i]);
                if (i < trait.contradictions.Count() - 1)
                {
                    stringBuilder.Append(LIST_MID);
                }
                else
                {
                    stringBuilder.Append(LIST_TAIL);
                }
            }
            description = stringBuilder.ToString();
        }
        return description;
    }

    public string GenerateMoodDescription(List<(string, int)> moodsValues)
    {
        const string MOOD_HEAD = "En cuanto a tu estado de ánimo actual,";

        List<(string, int)> positiveMoods = moodsValues.Where(mood => mood.Item2 > 0).ToList();
        if (positiveMoods.Count > 0)
        {
            stringBuilder.Clear();
            stringBuilder.Append($"{MOOD_HEAD} ");
            for (int i = 0; i < positiveMoods.Count(); ++i)
            {
                var moodValues = positiveMoods[i];
                int value = moodValues.Item2;
                --value;

                Mood mood = moods[moodValues.Item1];
                stringBuilder.Append($"{moodTerms[value]} {mood.adjective}");
                if (i < positiveMoods.Count() - 1)
                {
                    stringBuilder.Append(LIST_MID);
                }
                else
                {
                    stringBuilder.Append(LIST_TAIL);
                }
            }
            return stringBuilder.ToString();
        }
        return "";
    }
}
