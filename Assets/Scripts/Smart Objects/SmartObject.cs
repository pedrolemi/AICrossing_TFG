using System.Collections.Generic;
using UnityEngine;

// Clase que es contenedor de las interacciones del objeto
// Por ejemplo, una TV se puede encender, cambiar de canal...
public class SmartObject : MonoBehaviour
{
    //[SerializeField]
    //private string displayName;

    protected Dictionary<string, BaseInteraction> interactions;

    protected virtual void Start()
    {
        interactions = new Dictionary<string, BaseInteraction>();
        // Se guardan todas las interacciones del objeto
        List<BaseInteraction> interactionsAux = new List<BaseInteraction>(GetComponents<BaseInteraction>());
        foreach (BaseInteraction interaction in interactionsAux)
        {
            interactions.Add(interaction.displayName, interaction);
        }
    }

    public BaseInteraction GetInteraction(string name)
    {
        if (interactions.ContainsKey(name))
        {
            return interactions[name];
        }
        return null;
    }
}
