using UnityEngine;

public class MainMenuButtons : MonoBehaviour
{
    GameManager gameManager;

    [SerializeField]
    [TextArea(5, 10)]
    public string transitionText = "";           // Texto a mostrar en la pantalla de transicion

    // Start is called before the first frame update
    void Start()
    {
        gameManager = GameManager.Instance;
    }


    public void StartSettingsScene()
    {
        gameManager.ChangeToScene("Settings", false);
    }

    public void StartTextTransitionScene()
    {
        if (string.IsNullOrEmpty(gameManager.RemoteOptions.apiKey) && string.IsNullOrEmpty(gameManager.LocalOptions.modelPath))
        {
            StartSettingsScene();
        }
        else
        {
            gameManager.TransitionText = transitionText;
            gameManager.SceneAfterTransition = "Game";
            gameManager.ChangeToScene("TextTransition");
        }
    }


    public void ExitGame()
    {
        Application.Quit();
    }

}
