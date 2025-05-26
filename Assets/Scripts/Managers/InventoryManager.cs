using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class InventoryManager : Singleton<InventoryManager>
{
    class AmountAndScrollItem
    {
        public AmountAndScrollItem(int amt, GameObject obj)
        {
            amount = amt;
            scrollItem = obj;
        }
        public int amount { get; set; }
        public GameObject scrollItem { get; set; }
    }

    const int MAX_STACK_AMOUNT = 999;

    // Objetos de la scrollView indexados por BaseItem correspondiente
    Dictionary<BaseItem, AmountAndScrollItem> items;

    // Progreso de las misiones de Gather indexadas por los objetos necesarios para completarlas
    Dictionary<BaseItem, Dictionary<Quest, int>> itemsToTrack;

    // BaseItems indexados por nombre (para eliminar los objetos con nombres especiales)
    Dictionary<string, BaseItem> itemsByName;

    [SerializeField]
    GameObject contentsObj;     // Objeto padre que contiene los elementos de la listView

    [SerializeField]
    GameObject itemPrefab;      // Prefab de cada elemento de la listView

    [SerializeField]
    GameObject interactionsButtons;                             // Botones de interaccion con los objetos
    RectTransform interactionButtonsTr;                         // Transform de los botones de interaccion
    const float INTERACTIONS_BUTTONS_OFFSET_X = 300.0f;         // Separacion entre los elementos del scrollview y los botones de interaccion

    [SerializeField]
    GameObject bgBlock;         // Objeto que bloquea el fondo

    BaseItem selectedItem;      // Objeto seleccionado
    BaseItem carriedItem;       // Objeto equipado

    QuestManager questManager;
    EventSystem eventSystem;

    CharacterAnimationController playerAnim;


    // TEST
    [SerializeField]
    List<BaseItem> test;


    // Start is called before the first frame update
    void Start()
    {
        items = new Dictionary<BaseItem, AmountAndScrollItem>();
        itemsToTrack = new Dictionary<BaseItem, Dictionary<Quest, int>>();
        itemsByName = new Dictionary<string, BaseItem>();

        interactionButtonsTr = interactionsButtons.GetComponent<RectTransform>();
        interactionsButtons.SetActive(false);
        bgBlock.SetActive(false);
        selectedItem = null;
        carriedItem = null;

        questManager = QuestManager.Instance;
        eventSystem = EventSystem.current;
        playerAnim = PlayerAnimationsManager.Instance.GetAnimatorController();
    }


    // TEST
    //void Update()
    //{
    //    if (Input.GetKeyDown(KeyCode.P))
    //    {
    //        foreach (var item in test)
    //        {
    //            AddItem(item, 1);
    //        }
    //    }
    //    else if (Input.GetKeyDown(KeyCode.O))
    //    {
    //        foreach (var item in test)
    //        {
    //            RemoveItem(item, 1);
    //        }
    //    }
    //}


    // Obtiene el numero de objetos del tipo indicado que se tienen. Si no se tiene ninguno, devuelve 0
    public int GetItemCount(BaseItem item)
    {
        if (items.ContainsKey(item))
        {
            return items[item].amount;
        }
        return 0;
    }

    // Anade al inventario el objeto indicado en la cantidad indicada
    public void AddItem(BaseItem item, int amount)
    {
        GameObject inventoryObj = null;

        // Si el objeto ya esta en el inventario, se modifica la cantidad
        if (items.ContainsKey(item))
        {
            items[item].amount += amount;
            inventoryObj = items[item].scrollItem;
        }
        // Si no estaba en el inventario
        else
        {
            // Se anade el elemento del objeto a la escena y a la lista
            inventoryObj = Instantiate(itemPrefab, contentsObj.transform);
            AmountAndScrollItem it = new AmountAndScrollItem(amount, inventoryObj);
            items.Add(item, it);
            itemsByName.Add(item.itemName, item);

            // Anade un listener al onClick del boton del elemento
            // (Se tiene que hacer desde codigo porque necesita el objeto como parametro)
            inventoryObj.GetComponent<Button>().onClick.AddListener(() =>
            {
                ClickItem(inventoryObj);
                selectedItem = item;
            });
        }

        // Se limita la cantidad del objeto a obtener
        if (items[item].amount > MAX_STACK_AMOUNT)
        {
            items[item].amount = MAX_STACK_AMOUNT;
        }

        // Se actualizan los textos del nombre y cantidad
        inventoryObj.GetComponent<InventoryItem>().UpdateInfo(item.itemName, items[item].amount);


        // Actualizar las misiones de Gather
        List<Tuple<BaseItem, Quest>> questsToRemove = new List<Tuple<BaseItem, Quest>>();
        if (itemsToTrack.ContainsKey(item))
        {
            // Actualiza el objetivo de la mision
            List<Quest> keys = new List<Quest>(itemsToTrack[item].Keys);
            for (int i = 0; i < keys.Count; i++)
            {
                Quest quest = keys[i];
                itemsToTrack[item][quest] += amount;

                // Si se ha cumplido el objetivo de la mision, la elimina de
                // la lista de  misiones y de la de misiones asociadas al objeto
                if (itemsToTrack[item][quest] >= quest.Item.Amount)
                {
                    questManager.CompleteGatherQuest(quest);
                    questsToRemove.Add(new Tuple<BaseItem, Quest>(item, quest));
                }
            }
            foreach (Tuple<BaseItem, Quest> t in questsToRemove)
            {
                itemsToTrack[t.Item1].Remove(t.Item2);
            }
            questsToRemove.Clear();
        }
    }

    // Elimina del inventario la cantidad indicada de un objeto
    public void RemoveItem(BaseItem item, int amount)
    {
        // Si el objeto ya esta en el inventario, se modifica la cantidad
        if (items.ContainsKey(item))
        {
            items[item].amount -= amount;

            // Si la cantidad es <= 0, se elimina de la escena y de la lista
            if (items[item].amount <= 0)
            {
                Destroy(items[item].scrollItem);
                items.Remove(item);
                itemsByName.Remove(item.itemName);

                // Si ya no hay objetos del tipo seleccionado
                if (!items.ContainsKey(item))
                {
                    // Si el objeto que estaba equipado es el que se esta elimnando, deja de llevarlo
                    if (carriedItem == item)
                    {
                        StopCarryingItem();
                    }

                    // Cierra el menu de interacciones
                    UnclickItem();
                }
            }
            // Si no, se actualizan los textos del nombre y cantidad
            else
            {
                items[item].scrollItem.GetComponent<InventoryItem>().UpdateInfo(item.itemName, items[item].amount);
            }
        }
    }

    // Elimina del inventario la cantidad indicada de un objeto por su nombre
    // (llamado al abandonar o completar una mision de Delivery, Retrieval o Lost Item)
    public void RemoveItemByName(string itemName, int amount)
    {
        if (itemsByName.ContainsKey(itemName))
        {
            // Busca el BaseItem asociado al nombre del objeto
            BaseItem item = itemsByName[itemName];
            if (item != null)
            {
                RemoveItem(item, amount);
            }
        }
    }


    // Tira el objeto selecionado si no es un objeto clave (al pulsar el boton de tirar)
    public void ThrowSelectedItem()
    {
        if (selectedItem != null && !selectedItem.keyItem)
        {
            RemoveItem(selectedItem, 1);
        }
    }

    // Equipa/desequipa el objeto seleccionado (al pulsar el boton de equipar)
    public void CarrySelectedItem()
    {
        // Si no se lleva otro o el equipado es otro distinto, lo equipa y reproduce la animacion de llevar algo
        if (carriedItem == null || carriedItem != selectedItem)
        {
            carriedItem = selectedItem;

            // Se tiene que llamar desde Instance porque segun la jerarquia de objetos, puede
            // que no este inicializado el PlayerAnimationsManager cuando se setea playerAnim)
            PlayerAnimationsManager.Instance.SetCarryingItemSprite(carriedItem.icon);
            playerAnim = PlayerAnimationsManager.Instance.GetAnimatorController();

            playerAnim.PlayAnimation(CharacterAnimationController.CharacterAnims.CARRY);
        }
        // En caso contrario desequipa el objeto
        else if (carriedItem == selectedItem)
        {
            StopCarryingItem();
        }
    }

    // Desequipa el objeto y vuelve a la animacion de idle
    public void StopCarryingItem()
    {
        carriedItem = null;

        // Se tiene que llamar desde Instance porque segun la jerarquia de objetos, puede
        // que no este inicializado el PlayerAnimationsManager cuando se setea playerAnim)
        PlayerAnimationsManager.Instance.SetCarryingItemSprite(null);
        playerAnim = PlayerAnimationsManager.Instance.GetAnimatorController();

        playerAnim.PlayAnimation(CharacterAnimationController.CharacterAnims.IDLE);

    }


    // Abre el menu de interacciones (al hacer click en un objeto desde el inventario)
    public void ClickItem(GameObject button)
    {
        RectTransform buttonTr = button.GetComponent<RectTransform>();

        if (buttonTr != null)
        {
            // Coloca los botones al lado del objeto seleccionado
            interactionButtonsTr.position = buttonTr.position + new Vector3(INTERACTIONS_BUTTONS_OFFSET_X * GetComponent<RectTransform>().localScale.x, 0.0f, 0.0f);

            // Deselecciona el elemento de UI que se tenga seleccionado
            // (para poder interactuar otra vez con el sin necesidad de quitar el raton de encima)
            eventSystem.SetSelectedGameObject(null);

            // Activa los botones de interaccion
            bgBlock.SetActive(true);
            interactionsButtons.SetActive(true);

        }
    }

    // Cierra el menu de interacciones (al hacer click fuera de un objeto del inventario)
    public void UnclickItem()
    {
        // Deselecciona el elemento de UI que se tenga seleccionado
        eventSystem.SetSelectedGameObject(null);

        // Desactiva los botones de interaccion
        bgBlock.SetActive(false);
        interactionsButtons.SetActive(false);

        // Se quita el objeto seleccionado
        selectedItem = null;
    }


    // Devuelve si el objetivo de la mision se ha cumplido
    public bool QuestConditionMet(BaseItem item, int amount)
    {
        // La mision no se ha completado si no hay objeto equipado, si el objeto a comprobar es null O
        // si el objeto equipado no es el que se pide O en el inventario no hay como minimo la cantidad indicada
        if (item == null || carriedItem == null || carriedItem.id != item.id || GetItemCount(item) < amount)
        {
            return false;
        }

        // Si no, se ha completado, por lo que elimina la cantidad de ese objeto y devuelve true
        RemoveItem(item, amount);
        StopCarryingItem();
        return true;
    }

    // Anade un objeto al que hacer seguimiento y la mision que lo requiere
    public void TrackItem(BaseItem item, Quest quest)
    {
        // Si el objeto no esta en la lista de objetos trackear, lo anade e inicializa la lista de misiones
        if (!itemsToTrack.ContainsKey(item))
        {
            itemsToTrack.Add(item, new Dictionary<Quest, int>());
        }

        // Si la mision no esta en la lista de misiones para ese objeto, la anade con progreso 0
        if (!itemsToTrack[item].ContainsKey(quest))
        {
            itemsToTrack[item].Add(quest, 0);
        }

    }
    public void StopTrackingItem(BaseItem item, Quest quest)
    {
        // Si el objeto esta en la lista de objetos trackear
        if (itemsToTrack.ContainsKey(item))
        {
            // Si la mision esta en la lista de misiones para ese objeto, la elimina
            if (itemsToTrack[item].ContainsKey(quest))
            {
                itemsToTrack[item].Remove(quest);
            }
        }
    }

}
