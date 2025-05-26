using UnityEngine;

public class SmartPerformer : MonoBehaviour
{
    [SerializeField]
    private GameObject progressBarObject;
    private InteractionProgressBar progressBar;

    private PlayerAnimationsManager playerAnims;
    private PlayerMovement movement;

    private void Start()
    {
        progressBar = progressBarObject.GetComponentInChildren<InteractionProgressBar>();
        playerAnims = GetComponent<PlayerAnimationsManager>();
        movement = GetComponent<PlayerMovement>();
    }

    public void SetProgressBar(float progress)
    {
        progressBar.SetProgress(progress);
    }

    public void PlayAnimation(CharacterAnimationController.CharacterAnims anim, bool move)
    {
        playerAnims.PlayAnimation(anim);
        movement.SetCanMove(move);
    }
}
