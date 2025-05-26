using UnityEngine;

[CreateAssetMenu(fileName = "Walk Around Action", menuName = "NPC Actions/Walk around")]
public class WalkAroundAction : NPCAction
{
    public int nTimes;

    public override ActionId GetActionId()
    {
        return ActionId.WALK_AROUND;
    }
}
