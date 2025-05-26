using PrimeTween;
using TMPro;
using UnityEngine;

public class TextTransition : MonoBehaviour
{
    GameManager gameManager;

    [SerializeField]
    TextMeshProUGUI text;          // TextMeshPro (con propiedad text) del texto a mostrar

    [SerializeField]
    TextMeshProUGUI infoText;      // TextMeshPro (con propiedad text) del texto de pulsar para continuar

    [SerializeField]
    float flickerTime = 1.0f;      // Tiempo que dura el parpadeo del texto de pulsar para continuar

    string nextScene;

    // Start is called before the first frame update
    void Start()
    {
        gameManager = GameManager.Instance;

        string config = gameManager.TransitionText;
        if (!string.IsNullOrEmpty(config))
        {
            text.text = config;
        }

        config = gameManager.SceneAfterTransition;
        if (!string.IsNullOrEmpty(config))
        {
            nextScene = config;
        }

        ShowInfoText();
    }

    public void StartGame()
    {
        gameManager.ChangeToScene(nextScene);
    }

    private void HideInfoText()
    {
        Tween.Alpha(infoText, 0.1f, flickerTime).OnComplete(() =>
        {
            ShowInfoText();
        });
    }

    private void ShowInfoText()
    {
        Tween.Alpha(infoText, 1.0f, flickerTime).OnComplete(() =>
        {
            HideInfoText();
        });
    }
}
