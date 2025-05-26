using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;

// Accion similar a su padre, pero la realiza un numero de veces, en vez de forma indefinida
[TaskCategory("NavMeshMovement2D")]
public class WalAroundNTimes2D : WalkAround2D
{
    public SharedInt nTimes = 3;

    private int times;

    public override void OnStart()
    {
        base.OnStart();
        times = 0;
    }

    public override TaskStatus OnUpdate()
    {
        if (times >= nTimes.Value)
        {
            return TaskStatus.Success;
        }
        return base.OnUpdate();
    }

    protected override void OnArrive()
    {
        base.OnArrive();
        ++times;
    }

    public override void OnReset()
    {
        base.OnReset();
        nTimes = 3;
    }
}
