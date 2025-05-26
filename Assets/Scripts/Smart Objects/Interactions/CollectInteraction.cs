using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Events;

public class CollectInteraction : SimpleInteraction
{
    const int END_ANIM_TIME = 500;

    [SerializeField]
    private CharacterAnimationController.CharacterAnims playAnimation = CharacterAnimationController.CharacterAnims.IDLE;
    [SerializeField]
    private CharacterAnimationController.CharacterAnims endAnimation = CharacterAnimationController.CharacterAnims.IDLE;

    protected override void OnStart(SmartPerformer performer)
    {
        performer.SetProgressBar(0.0f);
        StartAnim(performer);
        base.OnStart(performer);
    }

    protected override void OnProgress(SmartPerformer performer, float proportion, float accumulatedProportion)
    {
        performer.SetProgressBar(accumulatedProportion);
        base.OnProgress(performer, proportion, accumulatedProportion);
    }

    protected override void OnComplete(SmartPerformer performer, UnityAction<BaseInteraction> onComplete)
    {
        performer.SetProgressBar(1.0f);
        _ = StopAnim(performer);
        base.OnComplete(performer, onComplete);
    }

    public void StartAnim(SmartPerformer performer)
    {
        performer.PlayAnimation(playAnimation, false);
    }

    public async Task StopAnim(SmartPerformer performer)
    {
        await Task.Delay(END_ANIM_TIME);
        performer.PlayAnimation(endAnimation, true);
    }
}
