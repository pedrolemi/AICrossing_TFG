using PrimeTween;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using LLM;
using LLM.Groq;
using LLM.Llamafile;
using LLM.LlamaCpp;
using LLM.RAG;
using Utilities;

public class RemoteOptions
{
    public string apiKey = "";
    public Model model = Model.LLAMA_3_3_70B_VERSATILE;
}

public class LocalOptions
{
    public string modelPath = "";
    public ChatTemplateType fallbackChatTemplate = ChatTemplateType.CHAT_ML;
    public bool server = false;
}

public class RAGOptions
{
    public string apiKey = "";
}

public class GameManager : SingletonPersistent<GameManager>
{
    [SerializeField]
    private string persistentQuestsFolder = "PersistentQuests";

    [Header("LLM Options")]
    private LocalOptions localOptions = new LocalOptions();
    public LocalOptions LocalOptions { get => localOptions; set => localOptions = value; }
    private RemoteOptions remoteOptions = new RemoteOptions();
    public RemoteOptions RemoteOptions { get => remoteOptions; set => remoteOptions = value; }
    private RAGOptions ragOptions = new RAGOptions();
    public RAGOptions RagOptions { get => ragOptions; set => ragOptions = value; }

    [Header("Transition Options")]
    [SerializeField]
    private Image fadeInOut;
    [SerializeField]
    private float transitionDuration = 1.0f;

    // Texto a mostrar en la pantalla de transicion
    public string TransitionText { get; set; } = "";
    public string SceneAfterTransition { get; set; } = "Game";

    public string NPCQuestsPath { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        NPCQuestsPath = Path.Combine(Application.persistentDataPath, "Data", "NPC");

        string persistentQuestsPath = Path.Combine(Application.streamingAssetsPath, persistentQuestsFolder);
        DirectoryUtils.CopyDirectory(persistentQuestsPath, Application.persistentDataPath);
    }

    // Start is called before the first frame update
    void Start()
    {
        PrimeTweenConfig.warnZeroDuration = false;
        PrimeTweenConfig.warnTweenOnDisabledTarget = false;
        if (fadeInOut != null)
        {
            Tween.Alpha(fadeInOut, 0, 0);
            fadeInOut.gameObject.SetActive(false);
        }
    }

    public void ChangeToScene(string sceneName, bool fade = true)
    {
        float trDur = fade ? transitionDuration / 2.0f : 0.0f;

        if (fade)
        {
            fadeInOut.gameObject.SetActive(true);
        }
        Tween.Alpha(fadeInOut, 1.0f, trDur).OnComplete(() =>
        {
            SceneManager.LoadScene(sceneName);
            Tween.Alpha(fadeInOut, 0.0f, trDur).OnComplete(() =>
            {
                fadeInOut.gameObject.SetActive(false);
            });
        });
    }


    public void LoadLLM(RAGPinecone ragPinecone)
    {
        // En funcion de los seleccionado en la pantalla anterior, se activa un modo u otro
        // para los modelos de lenguaje
        LLMProvider[] llmProviders = FindObjectsOfType<LLMProvider>();
        // La opcion que destaca por encima del resto es el servidor remoto
        if (!string.IsNullOrEmpty(remoteOptions.apiKey))
        {
            foreach (LLMProvider provider in llmProviders)
            {
                provider.LLMLocation = LLMLocation.REMOTE_SERVER;
                provider.LoadGroq(remoteOptions.apiKey, remoteOptions.model);
            }
        }
        else
        {
            // Se comprueba que existe el modelo local
            if (File.Exists(localOptions.modelPath))
            {
                if (localOptions.server)
                {
                    GameObject llamafile = new GameObject("LocalServer");
                    LlamafileModel llamafileModel = llamafile.AddComponent<LlamafileModel>();
                    llamafileModel.Load(localOptions.modelPath, localOptions.fallbackChatTemplate, true);
                    foreach (LLMProvider provider in llmProviders)
                    {
                        provider.LLMLocation = LLMLocation.LOCAL_SERVER;
                        provider.LoadLlamafile(llamafileModel);
                    }
                }
                else
                {
                    GameObject llamaCpp = new GameObject("Local");
                    LlamaCppModel llamaCppModel = llamaCpp.AddComponent<LlamaCppModel>();
                    llamaCppModel.Load(localOptions.modelPath, localOptions.fallbackChatTemplate);
                    foreach (LLMProvider provider in llmProviders)
                    {
                        provider.LLMLocation = LLMLocation.LOCAL;
                        provider.LoadLlamaCpp(llamaCppModel);
                        Personality personality = provider.GetComponent<Personality>();
                        provider.SetSystemMessage(personality.RoleSummary);
                    }
                }
            }
        }

        if (ragPinecone != null && !string.IsNullOrEmpty(ragOptions.apiKey))
        {
            ragPinecone = FindObjectOfType<RAGPinecone>();
            _ = ragPinecone.Load(ragOptions.apiKey);
            foreach (LLMProvider provider in llmProviders)
            {
                provider.LoadPinecone(ragPinecone);
            }
        }
    }
}
