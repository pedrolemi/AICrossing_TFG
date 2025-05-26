using UnityEngine;
using UnityEngine.Events;

public class CollectItemInteraction : CollectInteraction
{
    [SerializeField]
    protected BaseItem collectableItem = null;

    protected override void OnStart(SmartPerformer performer)
    {
        InventoryManager.Instance.StopCarryingItem();
        base.OnStart(performer);
    }

    protected override void OnComplete(SmartPerformer performer, UnityAction<BaseInteraction> onComplete)
    {
        base.OnComplete(performer, onComplete);
        InventoryManager.Instance.AddItem(collectableItem, 1);
    }
}
