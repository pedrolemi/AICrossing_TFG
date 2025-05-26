using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Clase que ademas de contener las interacciones, las muestra cuando el personaje se acerca a ellas
public class SelectableSmartObject : SmartObject
{
    [SerializeField]
    private GameObject interactionButtonPrefab;

    [SerializeField]
    private GameObject selectableMenu;

    private SmartPerformer currentPerformer;

    protected override void Start()
    {
        base.Start();
        if (interactionButtonPrefab != null)
        {
            UnityAction<BaseInteraction> onEnd = (interaction) =>
            {
                interaction.UnlockInteraction(currentPerformer);
                currentPerformer = null;
                LevelManager.Instance.SetTimeScale(1.0f);
            };

            Transform selectableMenuTrans = selectableMenu.GetComponent<Transform>();
            foreach ((string displayName, BaseInteraction interaction) in interactions)
            {
                // Se crea un boton a traves del cual realizar la interaccion
                GameObject interactionButton = Instantiate(interactionButtonPrefab, selectableMenuTrans);
                interactionButton.name = $"{displayName}InteractionButton";

                // Se cambia el nombre del boton con el de la interaccion
                TextMeshProUGUI textMeshPro = interactionButton.GetComponentInChildren<TextMeshProUGUI>();
                textMeshPro.text = displayName;

                Button button = interactionButton.GetComponent<Button>();
                // Cuando se clica en el boton, se realiza la interaccion
                button.onClick.AddListener(() =>
                {
                    if (interaction.CanPerform() && currentPerformer != null)
                    {
                        // Se para el juego
                        LevelManager.Instance.SetTimeScale(0.0f);
                        EnableSelectableMenu(false);
                        // Se bloquea la interaccion
                        interaction.LockInteraction(currentPerformer);
                        // Se realiza la interaccion
                        interaction.Perform(currentPerformer, onEnd, onEnd);
                    }
                });
            }
            EnableSelectableMenu(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        SmartPerformer performer = collision.gameObject.GetComponent<SmartPerformer>();
        if (performer != null)
        {
            currentPerformer = performer;
            EnableSelectableMenu(true);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        currentPerformer = null;
        EnableSelectableMenu(false);
    }

    private void EnableSelectableMenu(bool enable)
    {
        selectableMenu.SetActive(enable);
    }
}
