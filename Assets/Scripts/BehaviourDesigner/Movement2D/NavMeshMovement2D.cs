using UnityEngine;
using BehaviorDesigner.Runtime.Tasks;
using BehaviorDesigner.Runtime;
using UnityEngine.AI;

// Clase abstracta que define una accion del arbol de comportamiento
public abstract class NavMeshMovement2D : Action
{
    private Vector2 targetPosition;

    private NavMeshAgent agent;
    private CharacterAnimationController animController;

    protected Transform trans;

    public SharedFloat speed = 2f;
    // Indica a que distancia se detiene
    public SharedFloat stoppingDistance = 1.5f;
    // Indica el maximo de distancia a la que buscar un punto, en caso de que el punto
    // al que se queria acceder no se encuentre dentro de la malla de navegacion
    public SharedFloat maxSampleDistance = 40f;
    // Indica si para calcular la distancia de parada se tiene en cuenta la malla de navegacion (true)
    // o la distancia al objetivo (false)
    public SharedBool pathOnly = true;

    public override void OnAwake()
    {
        agent = GetComponent<NavMeshAgent>();
        agent.updateRotation = false;
        agent.updateUpAxis = false;

        trans = GetComponent<Transform>();
        Vector3 currentPosition = trans.position;
        currentPosition.z = 0f;
        trans.position = currentPosition;

        animController = GetComponent<CharacterAnimationController>();
    }

    public override void OnStart()
    {
        targetPosition = Vector2.zero;
        // Importante setearlo de esta manera al tratarse de un movimiento en 2D
        agent.isStopped = false;
        agent.speed = speed.Value;
    }

    public override void OnEnd()
    {
        Stop();
    }

    public override void OnBehaviorComplete()
    {
        Stop();
    }

    public override void OnReset()
    {
        speed = 5f;
        stoppingDistance = 1.5f;
        maxSampleDistance = 40f;
        pathOnly = true;
    }

    protected bool SetDestination(Vector2 destination)
    {
        agent.isStopped = false;
        if (agent.SetDestination(destination))
        {
            targetPosition = destination;
            return true;
        }
        return false;
    }

    // Dada una posicion, encontrar la mas cercano, en caso de que dicha posicion
    // no se encuentre en la malla de navegacion
    protected bool SamplePosition(ref Vector2 position)
    {
        NavMeshHit hit;
        if (NavMesh.SamplePosition(position, out hit, maxSampleDistance.Value, NavMesh.AllAreas))
        {
            position = hit.position;
            return true;
        }
        return false;
    }

    protected bool HasArrived()
    {
        // Hay un camino calculandose, por lo tanto, no ha llegado a su destino
        if (agent.pathPending)
        {
            return false;
        }

        // Hay un camino establecido
        if (agent.hasPath)
        {
            // Se tiene en cuenta solo la malla de navegacion
            if (pathOnly.Value)
            {
                return agent.remainingDistance < stoppingDistance.Value;
            }
            // Se tiene en cuenta solo el objetivo
            else
            {
                return Vector2.Distance(trans.position, targetPosition) < stoppingDistance.Value;
            }
        }

        // No hay un camino calculandose ni un camino actual, por lo tanto, ha llegado a su destino
        return true;
    }

    protected void Stop()
    {
        if (agent.hasPath)
        {
            agent.isStopped = true;
            agent.ResetPath();
        }
    }

    protected int GetAgentOrientation()
    {
        return animController.IsFacingRight() ? 1 : -1;
    }
}