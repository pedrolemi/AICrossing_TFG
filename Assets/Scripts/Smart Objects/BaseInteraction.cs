using UnityEngine;
using UnityEngine.Events;

public abstract class BaseInteraction : MonoBehaviour
{
    public enum InteractionType
    {
        // Se realiza de inmediato
        Instantaneous = 0,
        // Se tarda un tiempo
        OverTime = 1,
    }

    public string displayName;

    [SerializeField]
    protected float duration = 0.0f;

    [SerializeField]
    protected InteractionType interactionType = InteractionType.Instantaneous;

    protected SmartObject smartObject;

    protected virtual void Start()
    {
        smartObject = GetComponent<SmartObject>();
    }

    // Comprobar si se puede realizar la interaccion
    public abstract bool CanPerform();

    // Bloquear la interaccion, es decir, cuando el usuario decide usarla
    public abstract void LockInteraction(SmartPerformer performer);

    // Realizar la interaccion
    public abstract void Perform(SmartPerformer performer, UnityAction<BaseInteraction> onComplete, UnityAction<BaseInteraction> onStop);

    // Desbloquear la interaccion
    public abstract void UnlockInteraction(SmartPerformer performer);
}