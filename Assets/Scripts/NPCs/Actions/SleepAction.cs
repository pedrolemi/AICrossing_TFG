using UnityEngine;

[CreateAssetMenu(fileName = "Sleep Action", menuName = "NPC Actions/Sleep Action")]
public class SleepAction : NPCAction
{
    public override ActionId GetActionId()
    {
        return ActionId.SLEEP;
    }
}
