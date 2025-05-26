using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class SimpleInteraction : BaseInteraction
{
    // Informacion acerca del personaje que esta realizando la interaccion
    private class PerformerInfo
    {
        // Tiempo que lleva realizandola
        public float elapsedTime;
        public UnityAction<BaseInteraction> onComplete;
        public UnityAction<BaseInteraction> onStop;
    }

    private List<SmartPerformer> performersToCleanup = new List<SmartPerformer>();
    private Dictionary<SmartPerformer, PerformerInfo> currentPerformers = new Dictionary<SmartPerformer, PerformerInfo>();
    protected int NumCurrentUsers => currentPerformers.Count;

    // Maximo numero de personas que pueden realizar a la vez la interaccion
    [SerializeField]
    private int maxSimultaneousUsers = 1;

    private void Update()
    {
        foreach ((SmartPerformer performer, PerformerInfo performerInfo) in currentPerformers)
        {
            // Si no tiene informacion, es que la accion es inmediata
            if (performerInfo != null)
            {
                if (CanStillPerform())
                {
                    float previousElapsedTime = performerInfo.elapsedTime;
                    performerInfo.elapsedTime = Mathf.Min(performerInfo.elapsedTime + Time.deltaTime, duration);

                    // Cuanto de la interaccion ha realizado en este tick [0-1]
                    float proportion = (performerInfo.elapsedTime - previousElapsedTime) / duration;
                    // Cuanto de la interaccion ha realizado hasta el momento [0-1]
                    float accumulatedProportion = performerInfo.elapsedTime / duration;

                    OnProgress(performer, proportion, accumulatedProportion);

                    if (performerInfo.elapsedTime >= duration)
                    {
                        OnComplete(performer, performerInfo.onComplete);
                    }
                }
                else
                {
                    OnStop(performer, performerInfo.onStop);
                }
            }
        }

        // Refresh de los usuarios que han terminado la interaccion
        foreach (SmartPerformer performer in performersToCleanup)
        {
            currentPerformers.Remove(performer);
        }
        performersToCleanup.Clear();
    }

    public override bool CanPerform()
    {
        return NumCurrentUsers < maxSimultaneousUsers;
    }

    protected bool CanStillPerform()
    {
        return true;
    }

    public override void Perform(SmartPerformer performer, UnityAction<BaseInteraction> onComplete, UnityAction<BaseInteraction> onStop)
    {
        if (currentPerformers.ContainsKey(performer))
        {
            switch (interactionType)
            {
                case InteractionType.Instantaneous:
                    OnStart(performer);
                    OnComplete(performer, onComplete);
                    break;
                case InteractionType.OverTime:
                    currentPerformers[performer] = new PerformerInfo()
                    {
                        elapsedTime = 0,
                        onComplete = onComplete,
                        onStop = onStop
                    };
                    OnStart(performer);
                    break;
            }
        }
    }

    public override void LockInteraction(SmartPerformer performer)
    {
        if (!currentPerformers.ContainsKey(performer) && CanPerform())
        {
            Debug.Log($"{performer.name} has locked interaction {displayName}.");

            currentPerformers.Add(performer, null);
        }
    }

    public override void UnlockInteraction(SmartPerformer performer)
    {
        if (currentPerformers.ContainsKey(performer))
        {
            Debug.Log($"{performer.name} has unlocked interaction {displayName}.");

            performersToCleanup.Add(performer);
        }
    }

    protected virtual void OnStart(SmartPerformer performer) { }

    protected virtual void OnProgress(SmartPerformer performer, float proportion, float accumulatedProportion) { }

    protected virtual void OnComplete(SmartPerformer performer, UnityAction<BaseInteraction> onComplete)
    {
        onComplete?.Invoke(this);

        if (!performersToCleanup.Contains(performer))
        {
            performersToCleanup.Add(performer);
            Debug.LogWarning($"{performer.name} didn't unlock interaction {displayName} in their OnComplete.");
        }
    }

    protected virtual void OnStop(SmartPerformer performer, UnityAction<BaseInteraction> onStop)
    {
        onStop?.Invoke(this);

        if (!performersToCleanup.Contains(performer))
        {
            performersToCleanup.Add(performer);
            Debug.LogWarning($"{performer.name} didn't unlock interaction {displayName} in their OnStop.");
        }
    }
}
