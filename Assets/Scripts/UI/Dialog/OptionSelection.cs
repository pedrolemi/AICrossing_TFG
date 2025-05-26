using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class OptionSelection : MonoBehaviour
{
    [SerializeField]
    GameObject bg;
    [SerializeField]
    GameObject optionButton;

    [SerializeField]
    GameObject optionsGroup;

    List<string> options;

    DialogManager dialogManager;

    // Start is called before the first frame update
    void Start()
    {
        dialogManager = DialogManager.Instance;

        options = new List<string>();
    }

    // Anade las opciones indicadas en el array
    public void AddOptions(List<string> text, List<UnityEvent> actions)
    {
        // Solo se pueden anadir opciones si no hay opciones ya activadas
        if (!bg.activeSelf)
        {
            // Borra las opciones anteriores (por si acaso)
            ClearOptions();

            // Activa el fondo y el grupo de opciones
            bg.SetActive(true);
            optionsGroup.SetActive(true);

            // Anade una opcion por cada elemento del array
            for (int i = 0; i < text.Count; ++i)
            {
                UnityEvent action = null;
                if (actions.Count > i)
                {
                    action = actions[i];
                }
                string s = text[i];
                AddOption(s, action);
            }
        }
    }

    // Anade la opcion con el texto indicado
    private void AddOption(string text, UnityEvent action)
    {
        // Se instancia el boton y se anade al grupo de botones
        GameObject option = Instantiate(optionButton);
        option.transform.SetParent(optionsGroup.transform);

        // Se ajusta la escala del boton para que coincida con la del prefab (por si acaso la escala del canvas varia)
        option.transform.localScale = optionButton.transform.localScale;

        // Cambia el texto del boton
        option.GetComponentInChildren<TextMeshProUGUI>().text = text;

        // Anade al onClick del boton la funcion de seleccionar dicha opcion
        int index = options.Count;
        Button button = option.GetComponent<Button>();
        if (action != null)
        {
            button.onClick.AddListener(delegate () { action.Invoke(); });
        }
        button.onClick.AddListener(delegate () { SelectOption(index); });

        // Anade el texto a la lista de opciones
        options.Add(text);
    }

    // Oculta el menu de seleccion de opciones y las borra todas
    private void ClearOptions()
    {
        bg.SetActive(false);
        optionsGroup.SetActive(false);
        options.Clear();
        foreach (Transform option in optionsGroup.transform)
        {
            Destroy(option.gameObject);
        }
    }


    // Selecciona la opcion sobre la que se hace click
    public void SelectOption(int index)
    {
        // Cambia el siguiente nodo en el dialogManager
        dialogManager.SetNextNode(index);
        //Debug.Log(options[index]);

        // Se borran las opciones
        ClearOptions();
    }

}
