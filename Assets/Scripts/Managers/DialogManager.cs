using LLM;
using UnityEngine;

public class DialogManager : Singleton<DialogManager>
{
    [SerializeField]
    DialogBox dialogBox;

    [SerializeField]
    OptionSelection optionSelection;

    DialogNode currNode;
    int nextNodeIndex = 0;

    LLMProvider currLLmProvider;
    Personality currPersonality;

    LevelManager levelManager;


    void Start()
    {
        currNode = null;
        currLLmProvider = null;

        levelManager = LevelManager.Instance;
    }

    public bool IsActive()
    {
        return currNode != null;
    }

    // Cambia el nodo actual
    public void SetCurrNode(DialogNode n)
    {
        // Si no hay ningun nodo activo ni animacion reproduciendose
        if (currNode == null && !dialogBox.PlayingAnim())
        {
            // Reinicia el indice del nodo siguiente
            nextNodeIndex = 0;

            // Actualiza el nodo actual
            currNode = n;

            // Se procesa el nodo
            ProcessNode();
        }
    }

    // Actualiza el nodo actual y lo gestiona
    public void UpdateCurrNode()
    {
        if (currNode != null)
        {
            ProcessNextNode(GetNextNode());
        }
    }

    public void ProcessNextNode(DialogNode nextNode)
    {
        currNode = nextNode;
        ProcessNode();
    }

    // Procesa el nodo actual
    public void ProcessNode()
    {
        levelManager.SetTimeScale(currNode == null ? 1.0f : 0.0f);

        if (currNode != null)
        {
            // Resetea el nodo siguiente
            nextNodeIndex = 0;

            // Si es un nodo de texto (no pasa nada si es null porque entonces devolvera false)
            if (currNode is TextNode)
            {
                TextNode textNode = currNode as TextNode;

                // Se activa la caja de texto sin animacion (para poder actualizar correctamente el texto
                // y calcular las paginas, ya que si no esta activo, no se actualiza el numero de paginas)
                dialogBox.Activate(true, null, 0.0f);

                // Se establece el dialogo y se indica si se puede responder al nodo o no
                dialogBox.SetDialog(textNode.text, textNode.characterName);
                dialogBox.SetCanAnswer(textNode.canAnswer);

                // Se desactiva la caja de texto sin animacion para que sea instantaneo y se vuelve a activar con animacion
                dialogBox.Activate(false, null, 0.0f);
                dialogBox.Activate(true);
            }
            // Si es un nodo de opcion multiple
            else if (currNode is ChoiceNode)
            {
                ChoiceNode choiceNode = currNode as ChoiceNode;

                // Anade las opciones del nodo
                optionSelection.AddOptions(choiceNode.choices, choiceNode.actions);
            }
            else if (currNode is LLMNode)
            {
                LLMNode LLmNode = currNode as LLMNode;
                DialogNode nextNode = LLmNode.GenerateNextNode(currLLmProvider, currPersonality, dialogBox);
                //currNode.nextNodes.Insert(0, nextNode);
                currNode.AddNextNode(nextNode);
                SetNextNode(0);
            }
        }
    }

    // Termina de procesar el nodo actual (para nodos de texto por si despues va otro nodo de texto)
    public void EndCurrNode()
    {
        // Obtiene el nodo siguiente
        DialogNode nextNode = GetNextNode();

        // Si el nodo siguiente es de texto
        if (nextNode is TextNode && currNode is TextNode)
        {
            TextNode currTextNode = currNode as TextNode;
            TextNode nextTextNode = nextNode as TextNode;

            // Si el siguiente personaje que va a hablar es el mismo que el actual
            if (currTextNode.characterName == nextTextNode.characterName)
            {
                // Actualiza el nodo y cambia el dialogo y si se puede responder
                currNode = nextNode;
                dialogBox.SetDialog(currTextNode.text, currTextNode.characterName, currTextNode.canAnswer);
            }
            // Si es otro distitno, se desactiva la caja con animacion (y el evento de
            // animacion que se reproduce al acabar la animacion de ocultar la caja
            // sera el que se encargue de avisar que se gestione el siguiente nodo)
            else
            {
                dialogBox.Activate(false, () =>
                {
                    ProcessNextNode(nextNode);
                });
            }
        }
        // Lo mismo que antes
        else
        {
            dialogBox.Activate(false, () =>
            {
                ProcessNextNode(nextNode);
            });
        }
    }


    // Cambia el indice del siguiente nodo y lo actualiza (usado al elegir una opcion en la seleccion multiple)
    public void SetNextNode(int i)
    {
        nextNodeIndex = i;
        UpdateCurrNode();
    }


    // Devuelve el siguiente nodo segun el indice
    private DialogNode GetNextNode()
    {
        DialogNode nextNode = null;

        // Habra siguiente nodo si hay nodos en el array de nodos siguiente
        // y si el nodo actual es menor que el tamano del array - 1
        if (currNode.GetNextNodesLength() > 0 && nextNodeIndex < currNode.GetNextNodesLength())
        {
            nextNode = currNode.GetNextNodeByIndex(nextNodeIndex);
        }

        return nextNode;
    }


    // Crea un nodo de LLM y lo configura
    public void GenerateLLMNode(string query, bool useTools = false, bool enableRag = true, bool avoidQuestions = false, float questionProbability = 30.0f)
    {
        LLMNode llmNode = ScriptableObject.CreateInstance<LLMNode>();
        llmNode.query = query;
        llmNode.useTools = useTools;
        llmNode.enableRag = enableRag;
        llmNode.avoidQuestions = avoidQuestions;
        llmNode.questionProbability = questionProbability;

        if (currNode != null)
        {
            currNode.AddNextNode(llmNode);
            SetNextNode(0);
        }
        else
        {
            SetCurrNode(llmNode);
        }
    }

    public void SetCurrNPC(LLMProvider npcLLMProvider, Personality personaliy)
    {
        currLLmProvider = npcLLMProvider;
        currPersonality = personaliy;
    }
}
