using UnityEngine;

[CreateAssetMenu(fileName = "Go Wander Action", menuName = "NPC Actions/Go Wander Action")]
public class GoWanderAction : NPCAction
{
    public float duration;

    public override ActionId GetActionId()
    {
        return ActionId.GO_WANDER;
    }
}
