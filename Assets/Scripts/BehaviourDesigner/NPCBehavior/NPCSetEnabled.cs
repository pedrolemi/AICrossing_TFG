using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

[TaskCategory("NPCActions")]
public class NPCSetEnabled : Action
{
    public SharedBool enabled = false;

    private Collider2D collider;
    private CharacterAnimationController animController;
    private Personality personality;

    public override void OnAwake()
    {
        collider = GetComponent<Collider2D>();
        animController = GetComponent<CharacterAnimationController>();
        personality = GetComponent<Personality>();
    }

    public override TaskStatus OnUpdate()
    {
        if (collider != null)
        {
            collider.enabled = enabled.Value;
        }
        if (animController != null)
        {
            animController.SetEnabled(enabled.Value);
        }
        if(personality != null)
        {
            personality.SetNameTagEnabled(enabled.Value);
        }
        return TaskStatus.Success;
    }

    public override void OnReset()
    {
        enabled = false;
    }
}
