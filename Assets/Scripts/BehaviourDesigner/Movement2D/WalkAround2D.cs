using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

// Accion que indica al personaje que tiene que caminar hacia un punto en una zona dada
[TaskCategory("NavMeshMovement2D")]
public class WalkAround2D : WanderParent2D
{
    // Zona en la que buscar dicho punto
    public SharedGameObject boxGameObject = null;
    private BoxCollider2D boxCollider;

    public override void OnStart()
    {
        base.OnStart();

        boxCollider = null;
        if (boxGameObject.Value == null)
        {
            return;
        }
        else
        {
            boxCollider = boxGameObject.Value.GetComponent<BoxCollider2D>();
            if (boxCollider == null)
            {
                return;
            }
        }
    }

    public override TaskStatus OnUpdate()
    {
        if (boxGameObject.Value == null || boxCollider == null)
        {
            return TaskStatus.Failure;
        }

        return base.OnUpdate();
    }

    public override void OnReset()
    {
        base.OnReset();
        boxGameObject = null;
    }

    protected override bool TryGetTargetPosition(out Vector2 target)
    {
        target = new Vector2();

        bool found = false;
        int attempts = 0;
        while (attempts < maxTargetAttempts.Value && !found)
        {
            // Se atrata de encontrar un punto dentro del area
            Bounds bounds = boxCollider.bounds;
            target.x = Random.Range(bounds.min.x, bounds.max.x);
            target.y = Random.Range(bounds.min.y, bounds.max.y);

            // Se encuentra dentro de la malla 
            if (SamplePosition(ref target))
            {
                // Supera la distancia minima
                found = Vector2.Distance(trans.position, target) > minDistance.Value;
            }
            ++attempts;
        }
        return found;
    }
}
