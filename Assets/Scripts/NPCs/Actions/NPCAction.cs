using UnityEngine;

public abstract class NPCAction : ScriptableObject
{
    // El orden del enum determinar la rama del arbol de comportamiento al que corresponde
    public enum ActionId
    {
        GO,
        WALK_AROUND,
        GO_WANDER,
        SLEEP
    }

    [Range(0f, 1f)]
    public float probability;

    public string landmarkName;
    public GameObject landmark => LevelManager.Instance.GetLandmark(landmarkName);

    public abstract ActionId GetActionId();
}
