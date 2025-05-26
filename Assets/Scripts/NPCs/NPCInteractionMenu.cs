using UnityEngine;
using UnityEngine.UI;

public class NPCInteractionMenu : MonoBehaviour
{
    [SerializeField]
    GameObject contentsObj;     // Objeto padre que contiene los elementos de la lista
    [SerializeField]
    Color acceptedColor;        // Color del fondo del elemento de la scrollView al estar la mision aceptada

    NPCTriggerManager npcTriggerDetector;
    DialogManager dialogManager;
    QuestManager questManager;

    UIToggles uiToggles;

    [SerializeField]
    DialogNode test;

    // Start is called before the first frame update
    void Start()
    {
        npcTriggerDetector = NPCTriggerManager.Instance;
        dialogManager = DialogManager.Instance;
        questManager = QuestManager.Instance;

        uiToggles = GetComponent<UIToggles>();
        ClearQuestScrollView();
    }


    // Inicia una conversacion con el npc mas cercano
    // (llamado al pulsar el boton de hablar)
    public void Talk()
    {
        Personality personality = npcTriggerDetector.GetPersonality();
        dialogManager.SetCurrNPC(npcTriggerDetector.GetNPCProvider(), personality);

        //dialogManager.SetCurrNode(test);
        if (npcTriggerDetector.GetTalked(personality.DisplayName))
        {
            dialogManager.GenerateLLMNode("¡Hola de nuevo!", false, false, true);
        }
        else
        {
            dialogManager.GenerateLLMNode("¡Hola! Acabo de mudarme al pueblo y me gustaría presentarme. ¿Cuál es tu nombre?", false, false, true);
            npcTriggerDetector.SetTalked(personality.DisplayName);
        }
    }


    // Abre la lista de misiones disponibles del npc
    // (llamado al pulsar el boton de misiones)
    public void AskForQuest()
    {
        GameObject nearestNPC = npcTriggerDetector.GetNPC();
        if (nearestNPC != null)
        {
            NPCQuestHandler npcQuestHandler = nearestNPC.GetComponent<NPCQuestHandler>();

            if (npcQuestHandler != null)
            {
                // Si no se han completado misiones para ese npc y hay alguna mision
                if (!npcQuestHandler.GetCompleted() && npcQuestHandler.GetQuestList().Count > 0)
                {
                    // Recorre todas las misiones que tiene
                    foreach (Quest quest in npcQuestHandler.GetQuestList())
                    {
                        // Anade la mision a la scrollView
                        GameObject questObj = questManager.AddScrollViewElement(quest, contentsObj.transform);

                        // Si la mision esta aceptada, se cambia el color del fondo
                        if (npcQuestHandler.CheckQuestAccepted(quest))
                        {
                            questObj.GetComponent<Image>().color = acceptedColor;
                        }

                        // Anade un listener al onClick del boton del elemento
                        // (Se tiene que hacer desde codigo porque el prefab tambien se usa para
                        // otras cosas que no necesitan llamar a esta funcion al hacer click)
                        questObj.GetComponent<Button>().onClick.AddListener(() =>
                        {
                            // Si la mision no ha sido aceptada
                            if (!npcQuestHandler.CheckQuestAccepted(quest))
                            {
                                // Se pone como aceptada y se anade a la lista de misiones del jugador
                                npcQuestHandler.SetQuestAccepted(quest, true);
                                questManager.AddQuest(quest);

                                // Recorre de nuevo todas las misiones, y si no son la mision actual, se
                                // ponen como no aceptadas y se eliminan de la lista de misiones del jugador
                                foreach (Quest quest2 in npcQuestHandler.GetQuestList())
                                {
                                    if (quest != quest2)
                                    {
                                        npcQuestHandler.SetQuestAccepted(quest2, false);
                                        questManager.RemoveQuest(quest2);
                                    }
                                }
                            }

                            // Se cierra el menu 
                            uiToggles.ToggleNPCQuestList();
                            ClearQuestScrollView();
                        });
                    }
                }
                else
                {
                    // Se cierra el menu
                    uiToggles.ToggleNPCQuestList();
                }

            }
        }
    }


    // Destruye todos los elementos de la scrollView
    public void ClearQuestScrollView()
    {
        for (int i = 0; i < contentsObj.transform.childCount; i++)
        {
            Destroy(contentsObj.transform.GetChild(i).gameObject);
        }
    }

}
