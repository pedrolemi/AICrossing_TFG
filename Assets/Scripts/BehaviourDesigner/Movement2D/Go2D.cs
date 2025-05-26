using UnityEngine;
using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

// Accion que indica al personaje que tiene que desplazarse a un lugar
[TaskCategory("NavMeshMovement2D")]
public class Go2D : NavMeshMovement2D
{
    public SharedGameObject target = null;
    public SharedVector2 targetPosition = Vector2.zero;

    private Transform targetTransform;

    public override void OnStart()
    {
        base.OnStart();

        targetTransform = null;
        if (target.Value != null)
        {
            targetTransform = target.Value.GetComponent<Transform>();
        }

        SetDestination(GetTargetPosition());
    }

    public override TaskStatus OnUpdate()
    {
        if (HasArrived())
        {
            return TaskStatus.Success;
        }

        SetDestination(GetTargetPosition());

        return TaskStatus.Running;
    }

    public override void OnReset()
    {
        base.OnReset();
        target = null;
        targetPosition = Vector2.zero;
    }

    private Vector2 GetTargetPosition()
    {
        if (target.Value != null)
        {
            return targetTransform.position;
        }
        return targetPosition.Value;
    }
}
