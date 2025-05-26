using UnityEngine;

[CreateAssetMenu(fileName = "Go Action", menuName = "NPC Actions/Go Action")]
public class GoAction : NPCAction
{
    public override ActionId GetActionId()
    {
        return ActionId.GO;
    }
}
