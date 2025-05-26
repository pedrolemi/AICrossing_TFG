using UnityEngine;
using UnityEngine.Events;

public class CollectKeyItemInteraction : CollectItemInteraction
{
    protected override void OnComplete(SmartPerformer performer, UnityAction<BaseInteraction> onComplete)
    {
        base.OnComplete(performer, onComplete);
        Destroy(gameObject);
    }

    public void SetCollectableItem(BaseItem item)
    {
        collectableItem = item;
    }
}
