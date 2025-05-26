using UnityEngine;

public class LostItem : MonoBehaviour
{
    [SerializeField]
    SpriteRenderer sprite;

    [SerializeField]
    CollectKeyItemInteraction interaction;


    public void SetItem(BaseItem item)
    {
        sprite.sprite = item.icon;
        interaction.SetCollectableItem(item);
    }

}
