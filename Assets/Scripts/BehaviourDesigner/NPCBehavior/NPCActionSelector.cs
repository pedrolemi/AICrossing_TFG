using BehaviorDesigner.Runtime;
using BehaviorDesigner.Runtime.Tasks;
using System.Collections.Generic;
using UnityEngine;

// Selector que tiene una accion aleatoria del momento del dia correspondiente en base a probabilidades
[TaskCategory("NPCActions")]
public class NPCActionSelector : Composite
{
    // Momento del dia actual
    public SharedDayPart currentDayPart = null;
    public SharedInt maxAttempts = 4;
    public SharedGameObject target = null;
    public SharedFloat duration = 0f;
    public SharedInt nTimes = 0;

    private TaskStatus executionStatus;
    private NPCAction childAction;

    public override void OnAwake()
    {
        executionStatus = TaskStatus.Inactive;
    }

    public override void OnStart()
    {
        SetChild();
        FillParams();
        currentDayPart.Value.CurrentAction = childAction;
    }

    public override int CurrentChildIndex()
    {
        // La posicion en el enum corresponde con la accion en el arbol de comportamiento
        return (int)childAction.GetActionId();
    }

    public override bool CanExecute()
    {
        return executionStatus == TaskStatus.Inactive;
    }

    public override void OnChildExecuted(TaskStatus childStatus)
    {
        executionStatus = childStatus;
    }

    public override void OnConditionalAbort(int childIndex)
    {
        executionStatus = TaskStatus.Inactive;
        childAction = null;
        SetChild();
        FillParams();
    }

    public override void OnEnd()
    {
        executionStatus = TaskStatus.Inactive;
        childAction = null;
    }

    public override void OnReset()
    {
        currentDayPart = null;
        maxAttempts = 4;
        target = null;
        duration = 0f;
        nTimes = 0;
    }

    private void FillParams()
    {
        target.Value = null;
        duration.Value = 0f;
        nTimes.Value = 0;

        target.Value = childAction.landmark;
        // En funcion del tipo de accion, hay que asignar unos parametros u otros
        switch (childAction.GetActionId())
        {
            case NPCAction.ActionId.WALK_AROUND:
                WalkAroundAction walkAroundTask = childAction as WalkAroundAction;
                nTimes.Value = walkAroundTask.nTimes;
                break;
            case NPCAction.ActionId.GO_WANDER:
                GoWanderAction goAndWanderTask = childAction as GoWanderAction;
                duration.Value = goAndWanderTask.duration;
                break;
        }
    }

    private void SetChild()
    {
        if (currentDayPart.Value.CurrentAction != null)
        {
            childAction = currentDayPart.Value.CurrentAction;
        }
        else
        {
            ShuffleByProb();
        }
    }

    private void ShuffleByProb()
    {
        // Se obtiene una accion en base a probabilidades
        List<NPCAction> actions = currentDayPart.Value.actions;
        // Solo hay una accion
        if (actions.Count == 1)
        {
            childAction = actions[0];
        }
        else
        {
            List<NPCAction> possibleActions = new List<NPCAction>();
            int attempts = 0;
            while (possibleActions.Count <= 0 && attempts < maxAttempts.Value)
            {
                float randProb = Random.value;
                foreach (NPCAction task in actions)
                {
                    // Se obtienen las acciones cuya probabilidad por encima del rango aleatorio
                    if (randProb <= task.probability)
                    {
                        possibleActions.Add(task);
                    }
                }
                ++attempts;
            }

            if (possibleActions.Count <= 0)
            {
                childAction = actions[0];
            }
            else
            {
                // De todas las accions obtenidas en el paso anterior, se seleccioan una de forma aleatoria
                childAction = possibleActions[Random.Range(0, possibleActions.Count)];
            }
        }
    }
}
