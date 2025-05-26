using TMPro;
using UnityEngine;

public class QuestListItem : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI questTitle;       // TextMeshPro (con propiedad text) del titulo de la mision

    [SerializeField]
    TextMeshProUGUI giverName;        // TextMeshPro (con propiedad text) del nombre del npc que da la mision

    [SerializeField]
    TextMeshProUGUI description;      // TextMeshPro (con propiedad text) de la descripcion

    [SerializeField]
    TextMeshProUGUI objective;        // TextMeshPro (con propiedad text) del objetivo

    [SerializeField]
    TextMeshProUGUI reward;         // TextMeshPro (con propiedad text) de la recompensa

    // Actualiza el texto
    public void UpdateInfo(Quest quest, string itemName)
    {
        questTitle.text = quest.Title;
        giverName.text = quest.QuestGiverName;
        description.text = quest.Description;

        objective.text = "Objetivo: " + UpdateObjectiveText(quest, itemName);

        reward.text = "Amistad +" + quest.Reward.FriendshipPoints;
    }

    private string UpdateObjectiveText(Quest quest, string itemName)
    {
        // Si no hay nombre de receptor, es una mision de LostItem o de Gather
        if (string.IsNullOrEmpty(quest.ItemReceiverName))
        {
            // LostItem (tiene nombre de localizacion del objeto)
            if (!string.IsNullOrEmpty(quest.LocationName))
            {
                return $"Encontrar {itemName} de {quest.QuestGiverName} en {quest.LocationName}.";
            }
            // Gather
            else
            {
                return $"Recolectar {quest.Item.Amount} {itemName}.";
            }
        }
        // Si hay nombre de receptor pero no de proveedor, es una mision de Request
        else if (string.IsNullOrEmpty(quest.ItemProviderName))
        {
            return $"Entregar {quest.Item.Amount} {itemName} a {quest.ItemReceiverName}.";
        }
        // Si hay tanto nombre de receptor como de proveedor, es una mision de Delivery o de Retrieval
        else
        {
            // Si el el proveedor es el mismo que da la mision, es una mision de Delivery
            if (quest.ItemProviderName == quest.QuestGiverName)
            {
                return $"LLevar {quest.Item.Amount} {itemName} de {quest.QuestGiverName} a {quest.ItemReceiverName}.";

            }
            // Si no, es una mision de Retrieval
            else
            {
                return $"Pedir {quest.Item.Amount} {itemName} a {quest.ItemProviderName} para {quest.QuestGiverName}.";

            }
        }


    }

}