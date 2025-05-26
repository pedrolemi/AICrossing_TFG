using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

// Accion que indica al personaje que tiene que merodear de una forma u otra
[TaskCategory("NavMeshMovement2D")]
public abstract class WanderParent2D : NavMeshMovement2D
{
    // Distancia minima en la que encontrar el punto respecto desde donse se encuentra
    public SharedFloat minDistance = 3f;
    // Cuanto tiempo se para entre punto y punto
    public SharedFloat minPauseDuration = 0.0f;
    public SharedFloat maxPauseDuration = 0.0f;
    // Numero de intentos en los que buscar el punto
    public SharedInt maxTargetAttempts = 1;

    private bool waiting;
    private float elapsedTime;
    private float duration;

    public override void OnStart()
    {
        base.OnStart();

        // Inicialmente se espera
        waiting = true;
        elapsedTime = 0.0f;
        duration = Random.Range(minPauseDuration.Value, maxPauseDuration.Value);
    }

    public override TaskStatus OnUpdate()
    {
        if (waiting)
        {
            elapsedTime += Time.deltaTime;
            if (elapsedTime > duration)
            {
                // Se trata de encontrar un punto nuevo
                if (TryGetTargetPosition(out Vector2 target))
                {
                    elapsedTime = 0.0f;
                    waiting = false;
                    SetDestination(target);
                }
            }
        }
        else
        {
            // Si ha llegado al punto
            if (HasArrived())
            {
                OnArrive();
            }
        }

        return TaskStatus.Running;
    }

    protected virtual void OnArrive()
    {
        waiting = true;
        duration = Random.Range(minPauseDuration.Value, maxPauseDuration.Value);
    }

    public override void OnReset()
    {
        minDistance = 3f;
        minPauseDuration = 0f;
        maxPauseDuration = 0f;
        maxTargetAttempts = 1;
    }

    protected abstract bool TryGetTargetPosition(out Vector2 target);
}
