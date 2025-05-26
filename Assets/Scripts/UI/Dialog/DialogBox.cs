using UnityEngine;
using PrimeTween;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Unity.VisualScripting;

public class DialogBox : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI text;           // TextMeshPro (con propiedad text) de la caja de texto
    [SerializeField]
    TextMeshProUGUI nameTagText;    // TextMeshPro (con propiedad text) de la caja de nombre

    string fullText;                // Texto completo
    string currText;                // Texto mostrado hasta el momento

    List<int> lastCharIndexes;      // Indices del ultimo caracter de cada pagina (si el texto es muy largo, se divide automaticamente en paginas)
    int currPage;                   // Pagina actual
    int currChar;                   // Caracter actual
    bool writing;                   // Si se esta reproduciendo la animacion de mostrar los caracteres

    const float LAST_CHAR_COOLDOWN = 0.1f;  // Tiempo que pasa desde que se escribe el ultimo caracter
    float lastCharTime;
    bool lastPage;

    const float CHAR_DELAY = 0.03f;         // Retardo con el que aparece cada caracter
    float charTimer;                        // Temporizador para contar el tiempo que pasa desde que se escribe el ultimo caracter
    bool animateText;


    // Botones de navegacion
    [SerializeField]
    GameObject prevPageButton;      // Boton de pagina anterior
    [SerializeField]
    GameObject nextPageButton;     // Boton de pagina siguiente

    // Caja de input
    [SerializeField]
    GameObject inputButton;
    [SerializeField]
    GameObject inputBox;
    [SerializeField]
    GameObject submitButton;
    [SerializeField]
    TMP_InputField inputField;

    bool canAnswer;
    bool waitForText;

    EventSystem eventSystem;

    DialogManager dialogManager;

    // Tweens
    const float DEFAULT_ANIMATION_TIME = 0.3f;
    RectTransform rectTransform;
    bool onAnim;


    // Start is called before the first frame update
    void Start()
    {
        dialogManager = DialogManager.Instance;

        fullText = "";
        currText = "";

        lastCharIndexes = new List<int>();
        currPage = 0;
        currChar = 0;
        writing = false;

        charTimer = 0.0f;
        animateText = false;

        lastCharTime = LAST_CHAR_COOLDOWN;
        lastPage = false;

        canAnswer = false;
        waitForText = false;

        // Anade un listener al onValueChanged de la caja de input para que cuando se escriba, se compruebe el texto
        // (Se tiene que hacer desde codigo porque si se hace desde el editor, el texto no se actualiza hasta despues de llamar al evento)
        inputField.onValueChanged.AddListener(OnInputBoxTextChange);

        // Anade un listener al onSubmit de la caja de input para que se llame a submitInput cuando se pulsa el enter
        // (Se tiene que hacer desde codigo porque no esta en el editor. NO USAR onEndEdit PORQUE ESO TAMBIEN SE LLAMA AL PERDER EL FOCO)
        inputField.onSubmit.AddListener(SubmitInput);
        submitButton.GetComponent<Button>().onClick.AddListener(() =>
        {
            SubmitInput();
        });

        eventSystem = EventSystem.current;

        PrimeTweenConfig.warnZeroDuration = false;
        PrimeTweenConfig.warnEndValueEqualsCurrent = false;

        RectTransform prevPagTr = prevPageButton.GetComponent<RectTransform>();
        RectTransform nextPagTr = nextPageButton.GetComponent<RectTransform>();
        Tween.Position(prevPagTr, endValue: prevPagTr.position - new Vector3(0, 10, 0), duration: 0.5f, cycles: -1, cycleMode: CycleMode.Yoyo, ease: Ease.Linear);
        Tween.Position(nextPagTr, endValue: nextPagTr.position + new Vector3(0, 10, 0), duration: 0.5f, cycles: -1, cycleMode: CycleMode.Yoyo, ease: Ease.Linear);

        rectTransform = GetComponent<RectTransform>();
        Activate(false, null, 0.0f);

        onAnim = false;
    }

    // Update is called once per frame
    void Update()
    {
        //Si se esta escribiendo, hay texto que escribir
        if (writing && fullText.Length > 0)
        {
            if (animateText && lastCharIndexes.Count > 0)
            {
                // Si aun no se ha llegado al ultimo caracter de la pagina y el delay de escritura ya ha pasado
                if (currChar <= lastCharIndexes[currPage] && charTimer >= CHAR_DELAY)
                {
                    // Actualiza el texto que se muestra
                    currText += fullText[currChar];
                    currChar++;
                    text.text = currText;

                    // Se reinicia el contador para el delay
                    charTimer = 0;
                }
                // Si no, se actualiza el contador
                else
                {
                    charTimer += Time.deltaTime;
                }
            }



            // Si se ha paginado el texto
            if (lastCharIndexes.Count > 0)
            {
                // Si ya se han escrito todos los caracteres
                if (currChar > lastCharIndexes[currPage])
                {
                    if (waitForText)
                    {
                        currChar = 0;
                        currText = "";
                        text.text = currText;

                        charTimer = 0;
                    }
                    else
                    {
                        ShowNavigationButtons();
                    }
                }
            }


            if (lastPage && lastCharTime < LAST_CHAR_COOLDOWN)
            {
                lastCharTime += Time.deltaTime;
            }

            // Solo se puede saltar de pagina con las teclas si no se tiene ningun elemento de
            // UI (la caja de input) seleccionado y no se esta reproduciendo ninguna animacion
            if (eventSystem.currentSelectedGameObject == null && !onAnim && !waitForText)
            {
                if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown((int)MouseButton.Left))
                {
                    NextPage();
                }
                else if (Input.GetMouseButtonDown((int)MouseButton.Right))
                {
                    PrevPage();
                }
            }

        }
    }


    // Devuelve si se esta reproduciendo un tween
    public bool PlayingAnim()
    {
        return onAnim;
    }


    #region TEXT_SETTINGS
    public void WaitForText(string name)
    {
        SetDialog(". . .   ", name, true);
        waitForText = true;
    }


    // Cambia todo el texto actual de la caja de texto
    public void SetText(string newText)
    {
        if (!string.IsNullOrEmpty(newText))
        {
            waitForText = false;

            animateText = false;
            fullText = newText;
            text.text = newText;
        }
    }

    public void SetCanAnswer(bool answerable)
    {
        canAnswer = answerable;

        inputBox.SetActive(false);
        submitButton.SetActive(false);
        inputButton.SetActive(false);
    }

    // Necesario para mostrar correctamente el numero de paginas del texto, ya que si no
    // habria que esperar a que la mesh del texto cambiara y eso sucede en el siguiente update
    public void EndSetText(string response)
    {
        if (string.IsNullOrEmpty(response))
        {
            Debug.Log("No habla :(");
            dialogManager.EndCurrNode();
        }
        else
        {
            PaginateText();

            currChar = lastCharIndexes[currPage] + 1;
        }
    }

    // Cambia el dialogo entero con todas sus propiedades
    public void SetDialog(string newText, string name = null, bool animate = true)
    {
        if (!string.IsNullOrEmpty(newText))
        {
            waitForText = false;

            // La caja de texto con el nombre ajusta su ancho automaticamente para encajar con el nombre.
            // El objeto NameTag tiene un Horizontal Layout Group y un Content Size Fitter que permiten que
            // la caja use el ancho del objeto hijo (el que contiene el texto) para cambiar su propio ancho
            // De esta manera, por codigo no es necesario redimensionar nada, solo cambiar el texto
            if (!string.IsNullOrEmpty(name))
            {
                nameTagText.text = name;
            }


            writing = true;
            animateText = animate;

            // Reinicia los valores de las variables
            currText = "";
            currChar = 0;

            fullText = newText;
            text.text = newText;

            PaginateText();
            
            text.text = "";

            // Muestra el icono de volver a la pagina anterior y oculta el de pasar a la siguiente
            prevPageButton.SetActive(false);
            nextPageButton.SetActive(false);
        }
    }

    private void PaginateText()
    {
        currPage = 0;
        text.pageToDisplay = currPage + 1;

        lastCharTime = 0.0f;
        lastPage = false;

        lastCharIndexes.Clear();

        // Necesario para mostrar correctamente el numero de paginas del texto, ya que si no
        // habria que esperar a que la mesh del texto cambiara y eso sucede en el siguiente update
        text.ForceMeshUpdate();

        // Recorre todas las paginas y va guardando el indice del ultimo caracter de dicha pagina
        for (int i = 0; i < text.textInfo.pageCount; i++)
        {
            lastCharIndexes.Add(text.textInfo.pageInfo[i].lastCharacterIndex);
        }
    }
    #endregion


    #region PAGE_NAVIGATION

    // Muestra los botones de navegacion
    private void ShowNavigationButtons()
    {
        // Si no es la ultima pagina, se muestra el boton para pasar a la pagina siguiente
        if (currPage < lastCharIndexes.Count - 1)
        {
            nextPageButton.SetActive(true);
        }
        // Si lo es, se activa el boton para mostrar la caja de input si se puede responder a dicho dialogo
        else
        {
            nextPageButton.SetActive(false);
            inputButton.SetActive(canAnswer);
        }

        // Si la pagina a la que se pasa es la primera
        if (currPage == 0)
        {
            prevPageButton.SetActive(false);
        }
        else
        {
            prevPageButton.SetActive(true);
        }
    }

    // Pasa a la pagina siguiente
    public void NextPage()
    {
        // Deselecciona el elemento de UI que se tenga seleccionado (por si se ha pulsado el boton)
        eventSystem.SetSelectedGameObject(null);

        if (lastCharIndexes.Count > 0)
        {
            // Si aun no se ha llegado al ultimo caracter de la pagina
            if (currChar < lastCharIndexes[currPage])
            {
                // Muestra todo el texto de golpe
                ShowAllText();

                ShowNavigationButtons();
            }
            // Si se ha llegado al ultimo caracter de la pagina y sigue habiendo paginas que mostrar
            else if (currPage < lastCharIndexes.Count - 1)
            {
                currPage++;
                // Se borra el texto a mostrar y se pasa de pagina
                if (animateText)
                {
                    currText = "";
                    text.text = currText;
                }
                else
                {
                    text.pageToDisplay = currPage + 1;
                    currChar = lastCharIndexes[currPage] + 1;
                }

                // Se reinicia el contador para el delay
                charTimer = 0;

                ShowNavigationButtons();
            }
            // Si no, ya no habra mas texto que mostrar, por lo que se avisa al dialogManager que procese el nodo
            else
            {
                lastPage = true;
            }
        }
        else
        {
            lastPage = true;
        }

        if (lastPage && lastCharTime >= LAST_CHAR_COOLDOWN)
        {
            dialogManager.EndCurrNode();
        }
    }

    // Vuelve a la pagina anterior
    public void PrevPage()
    {
        // Deselecciona el elemento de UI que se tenga seleccionado (por si se ha pulsado el boton)
        eventSystem.SetSelectedGameObject(null);

        // Si no es la primera pagina
        if (currPage > 0)
        {
            currPage--;

            // Se borra el texto a mostrar y se vuelve a la pagina anterior
            if (animateText)
            {
                currText = "";
                text.text = currText;

                // Si la pagina a la que se pasa es la primera
                if (currPage == 0)
                {
                    // Se pone el caracter a escribir como el primero
                    currChar = 0;
                }
                // Si no,
                else
                {
                    // Se pone el caracter a escribir como el siguiente caracter al ultimo de la pagina anterior
                    currChar = lastCharIndexes[currPage - 1] + 1;
                }
            }
            else
            {
                text.pageToDisplay = currPage + 1;
                currChar = lastCharIndexes[currPage] + 1;
            }

            // Se reinicia el contador para el delay
            charTimer = 0;

            ShowNavigationButtons();
        }
    }

    // Actualiza el texto actual para mostrar el texto completo de la pagina
    private void ShowAllText()
    {
        if (animateText)
        {
            // Recorre todos los caracteres desde el actual hasta el
            // ultimo de la pagina anadiendo cada uno al texto actual
            for (int i = currChar; i <= lastCharIndexes[currPage]; i++)
            {
                currText += fullText[currChar];
                currChar++;
            }
            text.text = currText;
        }
    }

    #endregion


    // Mostrar/ocultar la caja de texto
    public void Activate(bool active, Action onComplete = null, float time = DEFAULT_ANIMATION_TIME)
    {
        onAnim = true;
        if (active)
        {
            inputBox.SetActive(false);
            submitButton.SetActive(false);
            inputButton.SetActive(false);
        }
        Tween.Scale(rectTransform, endValue: active ? Vector3.one : Vector3.zero, duration: time).OnComplete(() =>
        {
            onAnim = false;
            if (onComplete != null)
            {
                onComplete.Invoke();
            }
            writing = active;
            eventSystem.SetSelectedGameObject(null);
        });
    }



    // Activa/desactiva la caja de input y reinicia su texto
    public void ToggleInputBox()
    {
        inputBox.SetActive(!inputBox.activeSelf);
        inputField.SetTextWithoutNotify("");
    }

    // Activa/desactiva el boton de enviar mensaje
    private void OnInputBoxTextChange(string newText)
    {
        // El boton se mostrara si el mensaje no es vacio ni tiene puramente espacios en blanco
        submitButton.SetActive(!string.IsNullOrEmpty(newText) && !newText.Trim().Equals(""));
    }

    private void SubmitInput()
    {
        SubmitInput(inputField.text);
    }

    // Enviar el texto escrito en la caja de input
    private void SubmitInput(string newText)
    {
        // Solo se podra enviar el texto si el boton de enviar esta activo y no se estan reproduciendo las animaciones de transicion
        if (submitButton.activeSelf && !onAnim)
        {
            Activate(true);
            dialogManager.GenerateLLMNode(newText, false);
        }
    }

}
