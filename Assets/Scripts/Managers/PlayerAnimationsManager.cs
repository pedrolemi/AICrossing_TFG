using UnityEngine;
using static CharacterAnimationController;

public class PlayerAnimationsManager : Singleton<PlayerAnimationsManager>
{
    CharacterAnimationController anim;      // CharacterAnimationController del personaje del jugador

    [SerializeField]
    GameObject carryingItemParent;          // Objeto padre que contiene la mascara del objeto equipado                      
    [SerializeField]
    Animator maskAnimator;                  // Mascara del objeto equipado
    [SerializeField]
    SpriteRenderer carryingItemSprite;      // Sprite del objeto equipado


    // Start is called before the first frame update
    void Start()
    {
        anim = GetComponent<CharacterAnimationController>();
        maskAnimator.speed = 0.0f;
    }

    void Update()
    {
        // Si la animacion actual es la de llevar un objeto
        if (anim.GetCurrAnim() == CharacterAnims.CARRY)
        {
            // Hace que el frame de la animacion del objeto sea el mismo que el de la animacion actual
            maskAnimator.Play(0, 0, anim.GetCurrAnimFrame());

            // Flippea la animacion dependiendo de la direccion en la que mire
            // (al ser la animacion de la mascara una animacion que modifica el transform en una direccion
            // especifica, es necesario flippear el transform del padre para que la direccion en la que se
            // anima el transform tambien se flippee. Esto se consigue rotando el transform 180 grados)
            if (anim.IsFacingRight())
            {
                carryingItemParent.transform.localEulerAngles = new Vector3(0, 0, 0);
                carryingItemSprite.flipX = false;
            }
            else
            {
                carryingItemParent.transform.localEulerAngles = new Vector3(0, 180, 0);
                carryingItemSprite.flipX = true;
            }
        }
    }


    public CharacterAnimationController GetAnimatorController() { return anim; }

    public void PlayAnimation(CharacterAnims animation)
    {
        if (animation != CharacterAnims.CARRY)
        {
            carryingItemSprite.sprite = null;
        }
        anim.PlayAnimation(animation);
    }

    // Cambia el sprite del objeto equipado
    public void SetCarryingItemSprite(Sprite sprite)
    {
        carryingItemSprite.sprite = sprite;
    }
}
