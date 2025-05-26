using UnityEngine;
using static CharacterAnimationController;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField]
    float speed = 1.0f;                     // Velocidad caminando
    bool moving;

    [SerializeField]
    float runningMultiplier = 2.0f;         // Multiplicador de velocidad al correr
    bool running;                           // Si esta corriendo o no

    bool canMove;                           // Si se puede mover o no

    [SerializeField]
    float velocityMultiplier = 2.0f;             // Multiplicador que aplicarle a la velocidad del rigidbody

    Rigidbody2D rb;
    CharacterAnimationController anim;

    LevelManager levelManager;


    // Start is called before the first frame update
    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        anim = GetComponent<CharacterAnimationController>();

        moving = false;
        running = false;
        canMove = true;

        levelManager = LevelManager.Instance;
    }
    
        
    // Update is called once per frame
    void Update()
    {
        // Si el juego esta pausado o el personaje no se puede mover
        if (!canMove || levelManager.IsPaused())
        {
            // Si se esta reproduciendo la animacion de llevar algo, se pausa
            if (anim.GetCurrAnim() == CharacterAnims.CARRY)
            {
                anim.PauseAnimation();
            }
            // Si no, se pone la animacion de idle
            else if (canMove)
            {
                anim.PlayAnimation(CharacterAnims.IDLE);
            }

            return;
        }
        // Si no, se puede mover
        anim.ResumeAnimation();

        //transform.position += HandleMovement();

        // Reproduce la animacion correspondiente
        PlayAnimation();
    }

    private void FixedUpdate()
    {
        // Si el juego esta pausado o el personaje no se puede mover
        if (!canMove || levelManager.IsPaused())
        {
            rb.velocity = Vector3.zero;
            return;
        }

        // Mueve al personaje
        rb.velocity = HandleMovement() * velocityMultiplier;
    }

    // Gestiona las pulsaciones de tecla y el movimiento
    private Vector3 HandleMovement()
    {
        // Movimiento
        Vector3 movement = new Vector3(0, 0, 0);
        running = false;

        if (Input.GetKey(KeyCode.W))
        {
            movement += transform.up;
        }
        if (Input.GetKey(KeyCode.S))
        {
            movement -= transform.up;
        }
        if (Input.GetKey(KeyCode.D))
        {
            movement += transform.right;
            anim.FlipSpriteX(false);
        }
        if (Input.GetKey(KeyCode.A))
        {
            movement -= transform.right;
            anim.FlipSpriteX(true);
        }
        movement = movement.normalized * speed * Time.deltaTime;

        // Correr
        if (Input.GetKey(KeyCode.LeftShift))
        {
            movement *= runningMultiplier;
            running = true;
        }

        // Se esta moviendo si la magnitud del vector de movimiento no es 0
        moving = movement.magnitude > 0;

        return movement;
    }


    // Elige que animacion reproducir segun el estado del movimiento
    private void PlayAnimation()
    {
        // La siguiente animacion a reproducir por defecto es la de idle
        CharacterAnims nextAnim = CharacterAnims.IDLE;

        // Si esta corriendo y se esta moviendo, se pone la animacion de correr
        if (running && moving)
        {
            nextAnim = CharacterAnims.RUN;
        }
        // Si solo se esta moviendo, se pone la animacion de caminar
        else if (moving)
        {
            nextAnim = CharacterAnims.WALK;
        }
        // Si no se esta moviendo, ya esta por defecto la animacion de idle

        // Si no se esta reproduciendo la animacion de llevar algo, reproduce la animacion siguiente
        if (anim.GetCurrAnim() != CharacterAnims.CARRY)
        {
            anim.PlayAnimation(nextAnim);
        }
        // Si no, pausa la animacion y la reinicia para mostrar el primer frame
        // (ya que la animacion simula movimiento aunque el personaje este quieto)
        else if (!moving)
        {
            anim.PauseAnimation();
            anim.ResetAnimation();
        }
    }


    public void SetCanMove(bool can)
    {
        canMove = can;
    }

    public void ResumeMovement()
    {
        canMove = true;
    }
}
