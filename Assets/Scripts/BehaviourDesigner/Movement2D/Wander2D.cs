using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using UnityEngine;

// Accion que indica al personaje que tiene que caminar hacia un punto en sus alrededores, evitando obstaculos
[TaskCategory("NavMeshMovement2D")]
public class Wander2D : WanderParent2D
{
    public SharedFloat maxDistance = 3f;
    public SharedFloat maxDegrees = 30f;
    public SharedFloat collisionThreshold = 0.01f;

    public override void OnReset()
    {
        base.OnReset();
        maxDistance = 3f;
        maxDegrees = 30f;
        collisionThreshold = 0.01f;
    }

    protected override bool TryGetTargetPosition(out Vector2 target)
    {
        target = new Vector2();

        bool found = false;
        int attempts = 0;
        // Se obtiene la orientacion del personaje (izquierda o derecha)
        int agentOrientation = GetAgentOrientation();
        while (attempts < maxTargetAttempts.Value && !found)
        {
            // Se obtiene un punto aleatorio de su alrededor
            float distance = Random.Range(minDistance.Value, maxDistance.Value);
            float degrees = Random.Range(-maxDegrees.Value, maxDegrees.Value);
            float radians = degrees * Mathf.Deg2Rad;
            float cos = Mathf.Cos(radians);
            float sin = Mathf.Sin(radians);
            target.x = trans.position.x + agentOrientation * distance * cos;
            target.y = trans.position.y + distance * sin;
            Vector2 newTarget = target;
            if (SamplePosition(ref newTarget))
            {
                // Si la distancia entre el punto origiinal y el calculado es muy pequena,
                // es el mismo punto, entonces no hay problemas de colision
                float targetsDistance = Vector2.Distance(target, newTarget);
                if (targetsDistance < collisionThreshold.Value)
                {
                    target = newTarget;
                }
                // En cambio, si la distancia es muy grande, es que se ha tenicod que coger
                // otro punto porque el personaje podia llegar a chocarse
                // Por lo tanto, se coge el mismo punto, pero en el sentido opuesto
                else
                {
                    target.x = trans.position.x - agentOrientation * distance * cos;
                    target.y = newTarget.y;
                }
                found = true;
            }
            ++attempts;
        }
        return found;
    }
}
