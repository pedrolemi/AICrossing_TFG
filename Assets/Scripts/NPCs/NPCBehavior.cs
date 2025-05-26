using UnityEngine;
using UnityEngine.AI;
using BehaviorDesigner.Runtime;
using static CharacterAnimationController;
using System;
using System.Linq;
using System.Collections.Generic;

// Clase que indica una parte del dia
[Serializable]
public class DayPart
{
    // Momento en el que comienza
    [Range(0f, 24f)]
    public int startTime;
    // Acciones que el personaje puede realizar
    public List<NPCAction> actions;
    // Accion actual que esta realizando
    public NPCAction CurrentAction { get; set; }
}

public class NPCBehavior : MonoBehaviour
{
    private float ORIENTATION_THRESHOLD = 0.5f;

    // Un dia esta formado por multilples partes
    [SerializeField]
    private List<DayPart> day;

    private NavMeshAgent agent;
    private CharacterAnimationController animController;
    private BehaviorTree tree;

    // Indice que indica la parte del dia actual
    private int dayIndex;
    public DayPart currentDayPart => day[dayIndex];

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        // Hay que setearlo de esta forma al tratarse de malla de navegacion en 2D
        agent.updateRotation = false;
        agent.updateUpAxis = false;

        animController = GetComponent<CharacterAnimationController>();
        tree = GetComponent<BehaviorTree>();

        // Se ordena el dia, por si no se habia puesto de tal manera en el editor
        day = day.OrderBy((dayPeriod) => dayPeriod.startTime).ToList();

        UpdateStartDayPart();

        if (day.Count > 0)
        {
            LevelManager.Instance.AddNPCBehavior(this);
        }
        else
        {
            enabled = false;
            tree.enabled = false;
            tree.DisableBehavior();
        }
    }

    void Update()
    {
        SetAnimation();
        UpdateDayPart();
    }

    private void StopActions()
    {
        if (tree.enabled)
        {
            tree.enabled = false;
            tree.DisableBehavior();
        }
    }

    private void StartActions()
    {
        if (!tree.enabled)
        {
            tree.enabled = true;
            tree.EnableBehavior();
        }
    }

    private void RestartActions()
    {
        if (tree.enabled)
        {
            tree.DisableBehavior();
            tree.EnableBehavior();
        }
    }

    private void UpdateStartDayPart()
    {
        // Se encuentra la parte del dia inicial
        float currentHour = LevelManager.Instance.GetCurrentHour();
        for (int i = 0; i < day.Count(); ++i)
        {
            DayPart dayPart = day[i];
            if (dayPart.startTime > currentHour)
            {
                dayIndex = i - 1;
                if (dayIndex < 0)
                {
                    dayIndex = day.Count() - 1;
                }
                break;
            }
        }
    }

    private void UpdateDayPart()
    {
        if (day.Count() > 0)
        {
            float currentHour = LevelManager.Instance.GetCurrentHour();

            int nextIndex = (dayIndex + 1) % day.Count();
            if (nextIndex > 0 ||
                (nextIndex <= 0 && currentHour < currentDayPart.startTime))
            {
                if (currentHour >= day[nextIndex].startTime)
                {
                    currentDayPart.CurrentAction = null;
                    dayIndex = nextIndex;
                    RestartActions();
                }
            }
        }
    }

    public void TalkWith()
    {
        StopActions();
    }

    public void StopTalkingWith()
    {
        StartActions();
    }

    private void SetAnimation()
    {
        if (animController != null)
        {
            CharacterAnims anim = CharacterAnims.IDLE;
            // Si el personaje se esta moviendo, se cambia la animacion
            if (agent != null && agent.hasPath)
            {
                anim = CharacterAnims.WALK;
                Vector2 velocity = agent.velocity;
                float velocityX = Mathf.Abs(velocity.x);
                // En funcion de su velocidad, se determina la orientacion
                if (velocityX > ORIENTATION_THRESHOLD)
                {
                    bool rightFacing = velocity.x >= 0f ? true : false;
                    animController.FlipSpriteX(!rightFacing);
                }
            }
            animController.PlayAnimation(anim);
        }
    }
}
