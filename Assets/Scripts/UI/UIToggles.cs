using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using System.Linq;
using UnityEngine.UI;
using Utilities;

public class UIToggles : MonoBehaviour
{
    LevelManager levelManager;
    DialogManager dialogManager;
    QuestManager questManager;
    GameManager gameManager;

    [SerializeField]
    GameObject inventory;                   // Objeto con todos los elementos del inventario
    InventoryManager inventoryManager;

    [SerializeField]
    GameObject questsList;                      // Objeto con todos los elementos de la lista de misiones

    [SerializeField]
    GameObject npcQuestList;                // Objeto con todos los elemento de la lista de misiones de los npcs

    [SerializeField]
    GameObject relationshipList;                // Objeto con todos los elemento de la lista de misiones de los npcs

    [SerializeField]
    GameObject npcInteraction;                              // Objeto con todos los botones de interaccion con los npcs
    RectTransform npcInteractionRectTr;
    const float INTERACTIONS_BUTTONS_OFFSET_X = 60.0f;
    NPCTriggerManager npcTriggerManager;

    [SerializeField]
    Button questsButton;


    [SerializeField]
    GameObject pauseMenu;


    [Serializable]
    private class PossibleAnswers
    {
        public string[] possibleAnswers { get; set; }
    }
    List<string> questCompletedAnswers;


    EventSystem eventSystem;


    // Start is called before the first frame update
    void Start()
    {
        levelManager = LevelManager.Instance;
        dialogManager = DialogManager.Instance;
        inventoryManager = InventoryManager.Instance;
        npcTriggerManager = NPCTriggerManager.Instance;
        questManager = QuestManager.Instance;
        gameManager = GameManager.Instance;

        npcInteractionRectTr = npcInteraction.GetComponent<RectTransform>();

        DeactivateAll();

        questCompletedAnswers = new List<string>();

        // Se intenta abrir el json con las respuestas posibles al completar una mision
        /*
        string filePath = GameManager.Instance.QuestCompletedAnswersPath;
        //Si el archivo existe,
        if (File.Exists(filePath))
        {
            //Se lee entero como texto
            string fileText = File.ReadAllText(filePath);
        }
        */
        string fileText = Resources.Load<TextAsset>("quest_completed_answers").text;
        // Se deserializa para leer las respuestas posibles
        PossibleAnswers jsonAnswers = JsonSerializer.Deserialize<PossibleAnswers>(fileText);

        // Si se ha deserializado correctamente, se transforma el array de respuestas
        // en una lista y se cambia la lista actual por la del archivo
        if (jsonAnswers != null && jsonAnswers.possibleAnswers != null)
        {
            questCompletedAnswers = jsonAnswers.possibleAnswers.OfType<string>().ToList();
        }
        //}
        //else
        //{
        //    throw new Exception($"Quest completed answers file path doesn't exist: {filePath}");
        //}

        eventSystem = EventSystem.current;
    }

    // Update is called once per frame
    void Update()
    {
        // Si se pulsa la Q, hace toggle de la lista de misiones
        if (Input.GetKeyDown(KeyCode.Q))
        {
            ToggleQuestList();
            SetTimeScale();
        }
        // Si se pulsa la E, hace toggle del inventario
        else if (Input.GetKeyDown(KeyCode.E))
        {
            ToggleInventory();
            SetTimeScale();
        }
        // Si se pulsa la R, hace toggle de la lista de relaciones
        else if (Input.GetKeyDown(KeyCode.R))
        {
            ToggleRelationships();
            SetTimeScale();
        }
        // Si se pulsa el espacio, hace toggle del menu de interaccion con los npcs
        else if (Input.GetKeyDown(KeyCode.Space))
        {
            ToggleNPCInteractionButtons();
            SetTimeScale();
        }
        else if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePauseMenu();
            SetTimeScale();
        }
        else if ((dialogManager.IsActive() || npcInteraction.activeSelf || npcQuestList.activeSelf) && npcTriggerManager.GetNPC() == null)
        {
            DeactivateAll();
        }
    }


    // Devuelve si hay algun elemento de la UI activo
    public bool AnythingActive()
    {
        return dialogManager.IsActive() || inventory.activeSelf || questsList.activeSelf || npcInteraction.activeSelf || npcQuestList.activeSelf || relationshipList.activeSelf || pauseMenu.activeSelf;
    }

    // Desactiva todos los elementos
    private void DeactivateAll()
    {
        dialogManager.SetCurrNode(null);
        inventory.SetActive(false);
        questsList.SetActive(false);
        npcInteraction.SetActive(false);
        npcQuestList.SetActive(false);
        relationshipList.SetActive(false);
        pauseMenu.SetActive(false);

        levelManager.SetTimeScale(1.0f);
    }

    private void SetTimeScale()
    {
        // Se pausa el tiempo si hay algun elemento activo
        levelManager.SetTimeScale(AnythingActive() ? 0.0f : 1.0f);
        if (!AnythingActive())
        {
            eventSystem.SetSelectedGameObject(null);
        }
    }

    // Activa/desactiva la lista de misiones si no hay ningun otro elemento activo
    public void ToggleQuestList()
    {
        if (!AnythingActive() || questsList.activeSelf)
        {
            questsList.SetActive(!questsList.activeSelf);
        }

        SetTimeScale();
    }

    // Activa/desactiva el inventario si no hay ningun otro elemento activo
    public void ToggleInventory()
    {
        if (!AnythingActive() || inventory.activeSelf)
        {
            inventory.SetActive(!inventory.activeSelf);
            inventoryManager.UnclickItem();
        }

        SetTimeScale();
    }


    // Activa/desactiva la lista de misiones si no hay ningun otro elemento activo
    public void ToggleRelationships()
    {
        if (!AnythingActive() || relationshipList.activeSelf)
        {
            relationshipList.SetActive(!relationshipList.activeSelf);
        }

        SetTimeScale();
    }


    // Activa/desactiva la lista de misiones de los npcs si no hay ningun otro elemento activo
    public void ToggleNPCQuestList()
    {
        npcInteraction.SetActive(false);
        if (!AnythingActive() || npcQuestList.activeSelf)
        {
            npcQuestList.SetActive(!npcQuestList.activeSelf);
        }

        SetTimeScale();
    }


    // Activa/desactiva el menu de interaccion con los npcs si no hay ningun otro elemento activo
    public void ToggleNPCInteractionButtons()
    {
        GameObject currNPC = npcTriggerManager.GetNPC();

        if (currNPC != null && (!AnythingActive() || npcInteraction.activeSelf))
        {
            string currNPCName = npcTriggerManager.GetPersonality().DisplayName;

            // Si se esta abriendo el menu
            if (!npcInteraction.activeSelf)
            {
                Quest contextQuest = null;
                // Si hay alguna mision completada que necesite ese npc, se anade un nodo
                // de dialogo en el que el npc da las gracias por completar la mision
                if ((contextQuest = questManager.CheckQuestCompletedByNPC(currNPCName)) != null)
                {
                    string answer = "";
                    if (questCompletedAnswers.Count > 0)
                    {
                        answer = questCompletedAnswers[UnityEngine.Random.Range(0, questCompletedAnswers.Count)];
                    }
                    TextNode questCompletedNode = ScriptableObject.CreateInstance<TextNode>();
                    questCompletedNode.text = answer;
                    questCompletedNode.canAnswer = false;
                    questCompletedNode.characterName = currNPCName;
                    dialogManager.SetCurrNode(questCompletedNode);

                    // TODO: Anadir contexto al historial de dialogos de que se ha completado la quest (???)

                    return;
                }
                // Si no, si el npc puede dar algun objeto al jugador, se anade un nodo de dialogo indicandolo
                else if ((contextQuest = questManager.CheckItemsToRetreive(currNPCName)) != null)
                {
                    TextNode itemGivenNode = ScriptableObject.CreateInstance<TextNode>();

                    BaseItem item = contextQuest.Item.baseItem;
                    int amount = contextQuest.Item.Amount;
                    itemGivenNode.text = currNPCName + " te ha dado " + item.itemName;

                    itemGivenNode.canAnswer = false;
                    itemGivenNode.characterName = "Misiones";
                    dialogManager.SetCurrNode(itemGivenNode);

                    return;
                }

                NPCQuestHandler questHandler = currNPC.GetComponent<NPCQuestHandler>();
                if (questHandler != null)
                {
                    if (questHandler.GetCompleted())
                    {
                        questsButton.interactable = false;
                    }
                    else
                    {
                        questsButton.interactable = true;
                    }
                }
            }
            // Si no, se abre el menu de interaccion con el npc
            npcInteraction.SetActive(!npcInteraction.activeSelf);

            // Si se va a activar, recoloca los botones para que aparezcan al lado del npc
            if (npcInteraction.activeSelf)
            {

                npcInteractionRectTr.position =
                    Camera.main.WorldToScreenPoint(currNPC.transform.position) +
                    new Vector3(INTERACTIONS_BUTTONS_OFFSET_X * GetComponent<RectTransform>().localScale.x, 0.0f, 0.0f);
            }

        }

        SetTimeScale();
    }


    // Activa/desactiva la lista de misiones de los npcs si no hay ningun otro elemento activo
    public void TogglePauseMenu()
    {
        if (!AnythingActive() || pauseMenu.activeSelf)
        {
            pauseMenu.SetActive(!pauseMenu.activeSelf);
        }

        SetTimeScale();
    }
    public void ToMainMenu()
    {
        gameManager.ChangeToScene("MainMenu");
    }
}
