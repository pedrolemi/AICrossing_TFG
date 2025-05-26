using UnityEngine;
using UnityEngine.EventSystems;

public class ScrollViewDragEvents : MonoBehaviour
{
    EventSystem eventSystem;

    // Start is called before the first frame update
    void Start()
    {
        eventSystem = EventSystem.current;

        // Se anade el componente EventTrigger 
        EventTrigger eventTrigger = gameObject.AddComponent<EventTrigger>();

        // Se crea el evento para el EventTrigger de tipo EndDrag
        EventTrigger.Entry entry = new EventTrigger.Entry();
        entry.eventID = EventTriggerType.EndDrag;
        eventTrigger.triggers.Add(entry);

        // Se anade al evento el listener para que se deseleccionen todos los elementos al acabar el drag
        entry.callback.AddListener((data) => {
            eventSystem.SetSelectedGameObject(null);
        });
    }
}
