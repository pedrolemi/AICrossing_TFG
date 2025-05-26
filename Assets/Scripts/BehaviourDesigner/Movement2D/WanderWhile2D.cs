using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

// Accion similar a la de su padrer, pero la realiza durante un tiempo determinado
[TaskCategory("NavMeshMovement2D")]
public class WanderWhile2D : Wander2D
{
    public SharedFloat duration = 20f;

    private float elapsedTime;

    public override void OnStart()
    {
        base.OnStart();
        elapsedTime = 0f;
    }

    public override TaskStatus OnUpdate()
    {
        elapsedTime += Time.deltaTime;
        if (elapsedTime >= duration.Value)
        {
            return TaskStatus.Success;
        }
        return base.OnUpdate();
    }

    public override void OnReset()
    {
        base.OnReset();
        duration = 20f;
    }
}
