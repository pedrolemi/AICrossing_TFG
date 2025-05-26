using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using static BaseItem;

public class QuestManager : Singleton<QuestManager>
{
    [SerializeField]
    List<BaseItem> editorItemList;              // Lista de objetos que indexar (anadidos desde el editor)
    Dictionary<ITEMS, BaseItem> itemsById;      // Objetos indexados por su id

    // Misiones indexadas por nombre del NPC al que hablar para completar la mision
    // Cada mision tiene un objeto de scrollView asociado
    Dictionary<string, Dictionary<Quest, GameObject>> questsList;

    // Misiones de entregar objetos indexadas por nombre del NPC al que hablar para recoger el objeto
    // Cada mision tiene un objeto del inventario asociado
    Dictionary<string, Dictionary<Quest, BaseItem>> itemsToRetreive;

    [SerializeField]
    GameObject contentsObj;     // Objeto padre que contiene los elementos de la listView

    [SerializeField]
    GameObject itemPrefab;      // Prefab de cada elemento de la listView

    [SerializeField]
    GameObject lostItemPrefab;
    Dictionary<Quest, GameObject> lostItemByQuest;


    InventoryManager inventoryManager;
    DialogManager dialogManager;
    RelationshipsManager relationshipsManager;
    LevelManager levelManager;

    EventSystem eventSystem;


    // Start is called before the first frame update
    void Start()
    {
        // Recorre los objetos de la lista y los mete en el mapa segun su id
        itemsById = new Dictionary<ITEMS, BaseItem>();
        foreach (BaseItem it in editorItemList)
        {
            if (!itemsById.ContainsKey(it.id))
            {
                itemsById.Add(it.id, it);
            }
        }

        questsList = new Dictionary<string, Dictionary<Quest, GameObject>>();
        itemsToRetreive = new Dictionary<string, Dictionary<Quest, BaseItem>>();
        lostItemByQuest = new Dictionary<Quest, GameObject>();

        inventoryManager = InventoryManager.Instance;
        dialogManager = DialogManager.Instance;
        relationshipsManager = RelationshipsManager.Instance;
        levelManager = LevelManager.Instance;

        eventSystem = EventSystem.current;
    }


    public void AddQuest(Quest quest)
    {
        // Si la mision es null, no tiene item asociado, o no tiene recompensa, no se hace nada
        if (quest == null || quest.Item == null || quest.Reward == null)
        {
            return;
        }

        // Determina el nombre del npc con el que hablar para completar la mision
        string objectiveNPCName = quest.QuestGiverName;
        if (!string.IsNullOrEmpty(quest.ItemReceiverName))
        {
            objectiveNPCName = quest.ItemReceiverName;
        }

        // Si la mision no tiene npc con el que hablar para completarla, no se hace nada
        if (string.IsNullOrEmpty(objectiveNPCName))
        {
            return;
        }

        // Si el npc no esta en la lista de npcs, lo anade e inicializa la lista de misiones
        if (!questsList.ContainsKey(objectiveNPCName))
        {
            questsList.Add(objectiveNPCName, new Dictionary<Quest, GameObject>());
        }

        // Si la mision no esta en la lista de misiones de ese npc
        if (!questsList[objectiveNPCName].ContainsKey(quest))
        {
            GameObject questObj = AddScrollViewElement(quest, contentsObj.transform);

            // Anade un listener al onClick del boton del elemento
            // (Se tiene que hacer desde codigo porque no se puede llamar al EventSystem desde el editor)
            questObj.GetComponent<Button>().onClick.AddListener(() =>
            {
                eventSystem.SetSelectedGameObject(null);
            });


            // Si no hay nombre de receptor, es una mision de LostItem o de Gather
            if (string.IsNullOrEmpty(quest.ItemReceiverName))
            {
                // LostItem (tiene nombre de localizacion del objeto)
                GameObject landmark = levelManager.GetLandmark(quest.LocationName);
                if (!string.IsNullOrEmpty(quest.LocationName) && landmark != null)
                {
                    // Se spawnea el objeto
                    GameObject item = Instantiate(lostItemPrefab);
                    item.transform.position = landmark.transform.position;

                    lostItemByQuest.Add(quest, item);

                    // Se crea un nuevo objeto con otro nombre y que no se puede descartar
                    quest.Item.baseItem = CreateQuestItem(quest.QuestGiverName, quest.Item.baseItem);
                    item.GetComponent<LostItem>().SetItem(quest.Item.baseItem);
                }
                // Gather 
                // Se anade el objeto a recoger y la mision a la lista de seguimiento del inventario
                else
                {
                    inventoryManager.TrackItem(quest.Item.baseItem, quest);
                }
            }
            // Si hay nombre de receptor pero no de proveedor, es una mision de Request
            else if (string.IsNullOrEmpty(quest.ItemProviderName))
            {
                // No se tiene que hacer nada extra
            }
            // Si hay tanto nombre de receptor como de proveedor, es una mision de Delivery o de Retrieval
            else
            {
                // Se crea un nuevo objeto con otro nombre y que no se puede descartar
                quest.Item.baseItem = CreateQuestItem(quest.QuestGiverName, quest.Item.baseItem);

                // Si el el proveedor es el mismo que da la mision, es una mision de Delivery
                if (quest.ItemProviderName == quest.QuestGiverName)
                {
                    // Se anade directamente el objeto al inventario en la cantidad indicada
                    inventoryManager.AddItem(quest.Item.baseItem, quest.Item.Amount);
                }
                // Si no, es una mision de Retrieval
                else
                {
                    // Se anade a la lista el nombre del npc al que hablar para recibir el objeto
                    if (!itemsToRetreive.ContainsKey(quest.ItemProviderName))
                    {
                        itemsToRetreive.Add(quest.ItemProviderName, new Dictionary<Quest, BaseItem>());
                    }
                    // Si la mision no esta en la lista de objetos que tiene que dar ese npc, la anade
                    if (!itemsToRetreive[quest.ItemProviderName].ContainsKey(quest))
                    {
                        itemsToRetreive[quest.ItemProviderName].Add(quest, quest.Item.baseItem);
                    }
                }
            }


            // Anade la mision y el objeto de la listview a la lista del npc al que hablar para completar la mision
            questsList[objectiveNPCName].Add(quest, questObj);
        }
    }

    // Crea un objeto identico al que se pide, pero especifico para la mision y que no se puede tirar
    private BaseItem CreateQuestItem(string provider, BaseItem item)
    {
        BaseItem questItem = ScriptableObject.CreateInstance<BaseItem>();

        questItem.id = item.id;
        questItem.itemName = item.itemName + " de " + provider;
        questItem.icon = item.icon;
        questItem.keyItem = true;

        return questItem;
    }

    // Crea y anade la quest indicada a la listView indicada
    public GameObject AddScrollViewElement(Quest quest, Transform targetParent)
    {
        GameObject questObj = Instantiate(itemPrefab, targetParent.transform);

        BaseItem it = itemsById[(ITEMS)quest.Item.Id];
        quest.Item.baseItem = it;
        // Se actualizan los textos del objeto
        questObj.GetComponent<QuestListItem>().UpdateInfo(quest, quest.Item.Amount <= 1 ? it.itemName : it.pluralItemName);

        return questObj;
    }

    private void RemoveQuest(string objectiveNPCName, Quest quest)
    {
        if (questsList.ContainsKey(objectiveNPCName))
        {
            // Si la lista de misiones de ese npc contiene la mision
            if (questsList[objectiveNPCName].ContainsKey(quest))
            {
                // Si no hay nombre de receptor, es una mision de LostItem o de Gather
                if (string.IsNullOrEmpty(quest.ItemReceiverName))
                {
                    // LostItem (tiene nombre de localizacion del objeto)
                    if (!string.IsNullOrEmpty(quest.LocationName) && lostItemByQuest.ContainsKey(quest))
                    {
                        // Se despawnea el objeto
                        Destroy(lostItemByQuest[quest].gameObject);
                        lostItemByQuest.Remove(quest);

                        // Se obtiene el nombre del objeto a eliminar
                        string itemName = CreateQuestItem(quest.QuestGiverName, quest.Item.baseItem).itemName;

                        // Se elimina el objeto del inventario en la cantidad indicada
                        inventoryManager.RemoveItemByName(itemName, quest.Item.Amount);
                    }
                    // Gather 
                    // Se elimina el objeto a recoger de la lista de seguimiento del inventario
                    else
                    {
                        inventoryManager.StopTrackingItem(quest.Item.baseItem, quest);
                    }
                }
                // Si hay nombre de receptor pero no de proveedor, es una mision de Request
                else if (string.IsNullOrEmpty(quest.ItemProviderName))
                {
                    // No se tiene que hacer nada extra
                }
                // Si hay tanto nombre de receptor como de proveedor, es una mision de Delivery o de Retrieval
                else
                {
                    // Se obtiene el nombre del objeto a eliminar
                    string itemName = CreateQuestItem(quest.QuestGiverName, quest.Item.baseItem).itemName;

                    // Si el el proveedor es el mismo que da la mision, es una mision de Delivery
                    if (quest.ItemProviderName == quest.QuestGiverName)
                    {
                        // Se elimina el objeto del inventario en la cantidad indicada
                        inventoryManager.RemoveItemByName(itemName, quest.Item.Amount);
                    }
                    // Si no, es una mision de Retrieval
                    else
                    {
                        // Se elimina el objeto del npc al que hablar para recibir el objeto
                        if (itemsToRetreive.ContainsKey(quest.ItemProviderName))
                        {
                            itemsToRetreive[quest.ItemProviderName].Remove(quest);
                        }
                        inventoryManager.RemoveItemByName(itemName, quest.Item.Amount);
                    }
                }

                // Se elimina de la escena y de la lista de misiones del npc
                Destroy(questsList[objectiveNPCName][quest]);
                questsList[objectiveNPCName].Remove(quest);
            }
        }
    }
    public void RemoveQuest(Quest quest)
    {
        // Determina el nombre del npc con el que hablar para completar la mision
        string objectiveNPCName = quest.QuestGiverName;
        if (!string.IsNullOrEmpty(quest.ItemReceiverName))
        {
            objectiveNPCName = quest.ItemReceiverName;
        }

        RemoveQuest(objectiveNPCName, quest);
    }

    // Comprueba si hay alguna mision completa del npc especificado
    public Quest CheckQuestCompletedByNPC(string objectiveNPCName)
    {
        // Si la lista de npcs contiene el npc
        if (questsList.ContainsKey(objectiveNPCName))
        {
            // Recorre todas las misiones del npc
            foreach (Quest quest in questsList[objectiveNPCName].Keys)
            {
                // Si se ha completado la mision (el objeto esta equipado y hay suficientes unidades en el inventario)
                // se elimina de la lista de misiones y se obtiene la recompensa
                if (inventoryManager.QuestConditionMet(quest.Item.baseItem, quest.Item.Amount))
                {
                    CompleteQuest(objectiveNPCName, quest);

                    return quest;
                }
            }
        }

        return null;
    }

    // Completa una mision de Gather (llamado por el InventoryManager)
    public void CompleteGatherQuest(Quest quest)
    {
        // Determina el nombre del npc con el que hablar para completar la mision
        string objectiveNPCName = quest.QuestGiverName;
        if (!string.IsNullOrEmpty(quest.ItemReceiverName))
        {
            objectiveNPCName = quest.ItemReceiverName;
        }

        if (questsList.ContainsKey(objectiveNPCName))
        {
            // Si la lista de misiones de ese npc contiene la mision
            if (questsList[objectiveNPCName].ContainsKey(quest))
            {
                // Se indica que se ha completado la mision
                TextNode questCompletedNode = ScriptableObject.CreateInstance<TextNode>();
                string itemName = quest.Item.Amount > 1 ? quest.Item.baseItem.pluralItemName : quest.Item.baseItem.itemName;
                questCompletedNode.text = "Terminaste de recoger " + quest.Item.Amount + " " + itemName;
                questCompletedNode.canAnswer = false;
                questCompletedNode.characterName = "Misiones";
                dialogManager.SetCurrNode(questCompletedNode);

                CompleteQuest(objectiveNPCName, quest);
            }

        }
    }

    private void CompleteQuest(string objectiveNPCName, Quest quest)
    {
        // Se elimina de la escena y de la lista de misiones del npc
        RemoveQuest(objectiveNPCName, quest);

        quest.QuestHandler.SetCompleted(true);
        relationshipsManager.UpdateFriendship(quest.QuestGiverName, quest.Reward.FriendshipPoints);
    }

    // Comprueba si el npc indicado tiene algun objeto que entregar
    public Quest CheckItemsToRetreive(string objectiveNPCName)
    {
        // Si la lista de objetos a recoger contiene el npc, devuelve el primer objeto a recoger y anade al inventario la cantidad indicada
        if (itemsToRetreive.ContainsKey(objectiveNPCName))
        {
            if (itemsToRetreive[objectiveNPCName].Count > 0)
            {
                Quest quest = itemsToRetreive[objectiveNPCName].First().Key;

                inventoryManager.AddItem(quest.Item.baseItem, quest.Item.Amount);

                itemsToRetreive[objectiveNPCName].Remove(quest);

                return quest;
            }
        }

        return null;
    }


}
