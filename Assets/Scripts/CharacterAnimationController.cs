using System;
using System.ComponentModel;
using UnityEngine;
using Utilities;

public class CharacterAnimationController : MonoBehaviour
{
    public enum CharacterAnims
    {
        [Description("NONE")]
        NONE = -1,

        [Description("Idle")]
        IDLE,

        [Description("Walk")]
        WALK,

        [Description("Run")]
        RUN,

        [Description("Attack")]
        ATTACK,

        [Description("Axe")]
        AXE,

        [Description("Carry")]
        CARRY,

        [Description("Dig")]
        DIG,

        [Description("FishingThrow")]
        FISHING_THROW,

        [Description("FishingReel")]
        FISHING_REEL,

        [Description("FishingCaught")]
        FISHING_CAUGHT,

        [Description("Mine")]
        MINE,

        [Description("Interact")]
        INTERACT
    };

    [SerializeField]
    GameObject[] parts;

    SpriteRenderer[] partsRenderers;
    Animator[] partsAnimators;
    bool flipX;

    CharacterAnims currAnim = CharacterAnims.NONE;

    // Start is called before the first frame update
    void Start()
    {
        // Obtiene el renderer y el animator de todas las partes personalizables
        partsRenderers = new SpriteRenderer[parts.Length];
        partsAnimators = new Animator[parts.Length];
        for (int i = 0; i < parts.Length; i++)
        {
            partsRenderers[i] = parts[i].GetComponent<SpriteRenderer>();
            partsAnimators[i] = parts[i].GetComponent<Animator>();
        }

        // Reproduce la animacion de idle por defecto
        PlayAnimation(CharacterAnims.IDLE);

        // Si hay alguna parte del personaje que esta volteada en X, entonces...
        SpriteRenderer partFlippedX = Array.Find(partsRenderers, (part) => part.flipX);
        // El personaje esta voletado en X
        flipX = partFlippedX != null;
    }


    // Reproduce la animacion indicada en todas las partes personalizables
    public void PlayAnimation(CharacterAnims anim)
    {
        if (currAnim != anim)
        {
            foreach (Animator animator in partsAnimators)
            {
                animator.speed = 1.0f;
                animator.Play(anim.GetDescriptionCached());
            }
            currAnim = anim;
        }
    }

    public void PauseAnimation()
    {
        foreach (Animator animator in partsAnimators)
        {
            animator.speed = 0.0f;
        }
    }

    public void ResumeAnimation()
    {
        foreach (Animator animator in partsAnimators)
        {
            animator.speed = 1.0f;
        }
    }

    public void ResetAnimation()
    {
        foreach (Animator animator in partsAnimators)
        {
            animator.speed = 1.0f;
            animator.Play(currAnim.GetDescriptionCached(), -1, 0.0f);
        }
    }

    public CharacterAnims GetCurrAnim()
    {
        return currAnim;
    }

    public float GetCurrAnimFrame()
    {
        if (partsAnimators.Length <= 0)
        {
            return 0.0f;
        }
        return partsAnimators[0].GetCurrentAnimatorStateInfo(0).normalizedTime;
    }

    public void FlipSpriteX(bool flip)
    {
        if (flipX != flip)
        {
            foreach (SpriteRenderer part in partsRenderers)
            {
                part.flipX = flip;
            }
            flipX = flip;
        }
    }

    public void FlipSpriteY(bool flip)
    {
        foreach (SpriteRenderer part in partsRenderers)
        {
            part.flipY = flip;
        }
    }


    // La orientacion por defecto del personaje es hacia la derecha
    // Por lo tanto, si flipX = false, esta mirando hacia la derecha
    public bool IsFacingRight()
    {
        return !flipX;
    }

    public void SetEnabled(bool enabled)
    {
        for (int i = 0; i < parts.Length; i++)
        {
            partsRenderers[i].enabled = enabled;
            partsAnimators[i].enabled = enabled;
        }
    }

    public void SetSpritesLayer(string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        foreach (SpriteRenderer part in partsRenderers)
        {
            part.gameObject.layer = layer;
        }
    }
}