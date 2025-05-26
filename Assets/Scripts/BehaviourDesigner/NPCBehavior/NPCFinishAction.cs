using BehaviorDesigner.Runtime.Tasks;

[TaskCategory("NPCActions")]
public class NPCFinishAction : Action
{
    public SharedDayPart currentDayPart = null;

    public override TaskStatus OnUpdate()
    {
        currentDayPart.Value.CurrentAction = null;
        return TaskStatus.Success;
    }

    public override void OnReset()
    {
        currentDayPart = null;
    }
}
