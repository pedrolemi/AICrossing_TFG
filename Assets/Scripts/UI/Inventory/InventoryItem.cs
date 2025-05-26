using TMPro;
using UnityEngine;

public class InventoryItem : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI itemName;       // TextMeshPro (con propiedad text) del nombre del objeto

    [SerializeField]
    TextMeshProUGUI amount;         // TextMeshPro (con propiedad text) de la cantidad de objetos del tipo

    // Actualiza el texto del nombre del objeto y la cantidad
    public void UpdateInfo(string name, int amt)
    {
        itemName.text = name;
        amount.text = "x" + amt;
    }
}