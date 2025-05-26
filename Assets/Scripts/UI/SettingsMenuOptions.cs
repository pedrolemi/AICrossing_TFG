using SFB;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using LLM;
using Utilities;
using LLM.Groq;
using UnityEngine.UI;
using System.IO;

public class SettingsMenuOptions : MonoBehaviour
{
    private class ApiKeys
    {
        public string GroqCloud { get; set; }
        public string Pinecone { get; set; }
    }

    GameManager gameManager;
    EventSystem eventSystem;

    [SerializeField]
    GameObject remoteTab;
    [SerializeField]
    GameObject localTab;
    [SerializeField]
    GameObject RAGTab;

    [SerializeField]
    TMP_Dropdown modelDropdown;
    [SerializeField]
    TMP_InputField apiKeyInputText;

    [SerializeField]
    TMP_Dropdown chatTemplatesDropdown;
    [SerializeField]
    TMP_InputField modelPathText;
    [SerializeField]
    Toggle useServer;

    [SerializeField]
    TMP_InputField pineconeApiKeyInputText;


    [SerializeField]
    float warningTime = 2.0f;
    [SerializeField]
    GameObject remoteOrLocalNotSelectedWarning;
    [SerializeField]
    GameObject remoteAndLocalSelectedWarning;
    [SerializeField]
    GameObject pineconeKeyNotSelectedWarning;

    GameObject[] warnings;

    // Start is called before the first frame update
    void Start()
    {
        gameManager = GameManager.Instance;
        eventSystem = EventSystem.current;
        eventSystem.SetSelectedGameObject(null);

        ApiKeys apiKeys = LoadApiKeys("api_keys.json");

        List<string> dropdownElements = new List<string>();

        // Remoto
        foreach (Model model in Enum.GetValues(typeof(Model)))
        {
            dropdownElements.Add(model.GetDescriptionCached());
        }
        modelDropdown.AddOptions(dropdownElements);

        if (apiKeys != null && apiKeys.GroqCloud != null)
        {
            gameManager.RemoteOptions.apiKey = apiKeys.GroqCloud.Trim();
        }
        apiKeyInputText.text = gameManager.RemoteOptions.apiKey;
        modelDropdown.value = (int)gameManager.RemoteOptions.model;

        dropdownElements.Clear();

        // Local
        foreach (ChatTemplateType ct in Enum.GetValues(typeof(ChatTemplateType)))
        {
            dropdownElements.Add(ct.GetDescriptionCached());
        }
        chatTemplatesDropdown.AddOptions(dropdownElements);

        chatTemplatesDropdown.value = (int)gameManager.LocalOptions.fallbackChatTemplate;
        modelPathText.text = gameManager.LocalOptions.modelPath;
        useServer.isOn = gameManager.LocalOptions.server;

        // RAG
        if (apiKeys != null && apiKeys.Pinecone != null)
        {
            gameManager.RagOptions.apiKey = apiKeys.Pinecone.Trim();
        }
        pineconeApiKeyInputText.text = gameManager.RagOptions.apiKey;

        warnings = new GameObject[] {
            remoteOrLocalNotSelectedWarning,
            remoteAndLocalSelectedWarning,
            pineconeKeyNotSelectedWarning
        };

        HideAllWarnings();
        ShowRemoteSettings();
    }

    private ApiKeys LoadApiKeys(string fileName)
    {
        string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        string path = Path.Combine(documentsPath, fileName);
        if (File.Exists(path))
        {
            string text = File.ReadAllText(path);
            return JsonSerializer.Deserialize<ApiKeys>(text);
        }
        return null;
    }

    private void HideAllWarnings()
    {
        foreach (GameObject warning in warnings)
        {
            warning.SetActive(false);
        }
    }

    public void ToMainMenu()
    {
        // Si no se han configurado ni el modelo remoto ni el local
        if (string.IsNullOrEmpty(gameManager.RemoteOptions.apiKey) && string.IsNullOrEmpty(gameManager.LocalOptions.modelPath))
        {
            HideAllWarnings();
            remoteOrLocalNotSelectedWarning.SetActive(true);
        }
        // Si no se ha configurado la api key de pinecone
        else if (string.IsNullOrEmpty(gameManager.RagOptions.apiKey))
        {
            HideAllWarnings();
            pineconeKeyNotSelectedWarning.SetActive(true);
        }
        else
        {
            HideAllWarnings();

            // Si se han configurado tanto el modelo remoto como el local
            if (!string.IsNullOrEmpty(gameManager.RemoteOptions.apiKey) && !string.IsNullOrEmpty(gameManager.LocalOptions.modelPath))
            {
                remoteAndLocalSelectedWarning.SetActive(true);
                eventSystem.enabled = false;
                StartCoroutine(ShowWarningAndReturn());
            }

            // Si solo se ha configurado uno de los modelos
            else
            {
                gameManager.ChangeToScene("MainMenu", false);
            }
        }

    }

    private IEnumerator ShowWarningAndReturn()
    {
        yield return new WaitForSeconds(warningTime);
        gameManager.ChangeToScene("MainMenu", true);
        eventSystem.enabled = true;
    }

    public void ShowRemoteSettings()
    {
        RAGTab.SetActive(false);
        remoteTab.SetActive(true);
        localTab.SetActive(false);
    }

    public void ShowLocalSettings()
    {
        RAGTab.SetActive(false);
        remoteTab.SetActive(false);
        localTab.SetActive(true);
    }
    public void ShowRAGSettings()
    {
        RAGTab.SetActive(true);
        remoteTab.SetActive(false);
        localTab.SetActive(false);
    }


    public void OpenGroqLink()
    {
        Application.OpenURL("https://console.groq.com/keys");
    }
    public void OnGroqApiKeyEditEnd()
    {
        gameManager.RemoteOptions.apiKey = apiKeyInputText.text;
    }
    public void OnChatModelSelectionChanged()
    {
        gameManager.RemoteOptions.model = (Model)modelDropdown.value;
    }


    public void OnFallbackTemplateSelectionChanged()
    {
        gameManager.LocalOptions.fallbackChatTemplate = (ChatTemplateType)chatTemplatesDropdown.value;
    }
    public void OpenModelFile()
    {
        string[] paths = StandaloneFileBrowser.OpenFilePanel("Modelo del LLM", "", "gguf", false);

        if (paths.Length > 0)
        {
            gameManager.LocalOptions.modelPath = paths[0];
            modelPathText.text = paths[0];
        }
        else
        {
            gameManager.LocalOptions.modelPath = "";
            modelPathText.text = "";
        }
        Invoke("StopInputEdit", 0.1f);
    }
    public void OnServerToggleChange()
    {
        gameManager.LocalOptions.server = useServer.isOn;
    }


    public void OpenPineconeLink()
    {
        Application.OpenURL("https://docs.pinecone.io/guides/projects/manage-api-keys");
    }
    public void OnPineconeApiKeyEditEnd()
    {
        gameManager.RagOptions.apiKey = pineconeApiKeyInputText.text;
    }

    private void StopInputEdit()
    {
        eventSystem.SetSelectedGameObject(null);
    }
}
