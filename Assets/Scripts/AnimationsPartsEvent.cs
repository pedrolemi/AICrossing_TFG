using UnityEngine;

public class AnimationsPartsEvent : MonoBehaviour
{
    [SerializeField]
    PlayerMovement playerMovement;

    public void EndAnim()
    {
        playerMovement.ResumeMovement();
    }
}
