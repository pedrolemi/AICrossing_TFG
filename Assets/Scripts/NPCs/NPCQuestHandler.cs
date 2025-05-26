using System;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using System.Linq;
using Utilities;

public class NPCQuestHandler : MonoBehaviour
{
    bool completed;                                 // Si ya se ha completado alguna mision para este npc

    List<Quest> quests;                             // Lista de misiones generadas
    Dictionary<Quest, bool> questsAcceptedSate;     // Estad de las misiones (aceptada, no aceptada)


    // Start is called before the first frame update
    void Start()
    {
        completed = false;

        quests = new List<Quest>();
        questsAcceptedSate = new Dictionary<Quest, bool>();

        string npcName = "";
        Personality personality = GetComponent<Personality>();
        if (personality != null)
        {
            npcName = personality.DisplayName;
        }

        RelationshipsManager.Instance.UpdateFriendship(npcName, 0);

        // Se intenta abrir la carpeta del npc que contiene las misiones que puede dar
        string directory = Path.Combine(GameManager.Instance.NPCQuestsPath, npcName);
        // Si el directorio existe,
        if (Directory.Exists(directory))
        {
            // Lee todos los jsons del directorio
            List<string> files = Directory.GetFiles(directory, "*.json").ToList();

            // Recorre cada json
            foreach (string filePath in files)
            {
                // Se lee entero como texto
                string fileText = File.ReadAllText(filePath);

                // Se convierte en Quest y se anade a lista
                Quest quest = JsonSerializer.Deserialize<Quest>(fileText);
                quest.QuestHandler = this;

                quests.Add(quest);
                questsAcceptedSate.Add(quest, false);
            }

        }
        else
        {
            throw new Exception($"NPC quests path doesn't exist: {directory}.");
        }
    }


    public List<Quest> GetQuestList() { return quests; }
    public bool CheckQuestAccepted(Quest quest)
    {
        if (questsAcceptedSate.ContainsKey(quest))
        {
            return questsAcceptedSate[quest];
        }
        return false;
    }
    public void SetQuestAccepted(Quest quest, bool accepted)
    {
        if (questsAcceptedSate.ContainsKey(quest))
        {
            questsAcceptedSate[quest] = accepted;
        }
    }

    public bool GetCompleted() { return completed; }
    public void SetCompleted(bool comp) { completed = comp; }
}
