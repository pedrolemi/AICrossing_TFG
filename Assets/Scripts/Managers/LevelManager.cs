using LLM.RAG;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering.Universal;

public class LevelManager : Singleton<LevelManager>
{
    GameManager gameManager;

    [SerializeField]
    int maxDays = 5;                       // Numero de dias necesarios para acabar el juego
    [SerializeField]
    int minAvgFriendship = 5;             // Amistad media necesaria para ganar
    [SerializeField]
    [TextArea(5, 10)]
    string winTransitionText = "";           // Texto a mostrar en la pantalla de transicion al ganar
    [SerializeField]
    [TextArea(5, 10)]
    string loseTransitionText = "";         // Texto a mostrar en la pantalla de transicion al perder

    // Ciclo de dia
    [SerializeField]
    [Range(0, 24)]
    int initTime = 6;

    float prevTime;
    float hoursPassed;
    float hour;
    int day;

    float timeScale;

    [SerializeField]
    [Range(0, 5)]
    float speedFactor = 1.0f;

    [SerializeField]
    TextMeshProUGUI hourText;
    [SerializeField]
    TextMeshProUGUI dayText;

    [SerializeField]
    Gradient lightGradient;
    [SerializeField]
    Light2D lightSource;

    [SerializeField]
    int monospaceSize = 30;

    HashSet<NPCBehavior> npcsBehaviors;

    Dictionary<string, GameObject> landmarksByName;
    [SerializeField]
    GameObject landmarksParent;

    [SerializeField]
    private RAGPinecone ragPinecone;
    [SerializeField]
    private bool loadLLM = false;

    RelationshipsManager relationshipsManager;

    protected override void Awake()
    {
        base.Awake();
        prevTime = 0.0f;
        hoursPassed = 0.0f;
        hour = 0.0f;
        day = 1;
        prevTime = hoursPassed = hour = initTime;

        npcsBehaviors = new HashSet<NPCBehavior>();
    }

    // Start is called before the first frame update
    void Start()
    {
        gameManager = GameManager.Instance;
        timeScale = 1.0f;

        hourText.text = ToHour(hour);
        dayText.text = "Día " + day;

        if (lightSource != null)
        {
            ChangeLight(hour / 24.0f);
        }

        LoadLandmarks();

        if (loadLLM)
        {
            gameManager.LoadLLM(ragPinecone);
        }

        relationshipsManager = RelationshipsManager.Instance;
    }

    // Update is called once per frame
    void Update()
    {
        if (day <= maxDays)
        {
            UpdateHour();

            if (day > maxDays)
            {
                SetTimeScale(0.0f);
                hourText.text = "<mspace=" + monospaceSize.ToString() + "px>00:00</mspace>";

                Invoke("EndGame", 1.0f);
            }
        }
    }


    private void EndGame()
    {
        if (relationshipsManager.GetAverageFriendship() >= minAvgFriendship)
        {
            gameManager.TransitionText = winTransitionText;
        }
        else
        {
            gameManager.TransitionText = loseTransitionText;
        }

        gameManager.SceneAfterTransition = "MainMenu";
        gameManager.ChangeToScene("TextTransition");
    }


    public void SetTimeScale(float ts)
    {
        timeScale = ts;

        if (ts != 1.0f)
        {
            foreach (NPCBehavior npcBehavior in npcsBehaviors)
            {
                npcBehavior.TalkWith();

                NavMeshAgent agent = npcBehavior.gameObject.GetComponent<NavMeshAgent>();
                if (agent != null)
                {
                    agent.velocity = Vector3.zero;
                }
            }
        }
        else
        {
            foreach (NPCBehavior npcBehavior in npcsBehaviors)
            {
                npcBehavior.StopTalkingWith();
            }
        }
    }

    public bool IsPaused()
    {
        return timeScale <= 0.0f;
    }


    private void UpdateHour()
    {
        // Actualiza la hora y las horas que han pasado desde el inicio
        hour += Time.deltaTime * speedFactor * timeScale;
        hoursPassed += Time.deltaTime * speedFactor * timeScale;

        // El tiempo que ha pasado desde el ultimo guardado
        // supera las 24 horas, ha pasado un dia completo
        if ((hoursPassed - prevTime) >= 24)
        {
            // Se reinician las horas
            hoursPassed = prevTime = initTime;
            day++;
            dayText.text = "Día " + day;
        }
        hour %= 24;
        ChangeLight(hour / 24.0f);

        string stringText = ToHour(hour);

        // Se convierte el texto a monospace para que todos los numeros ocupen lo mismo
        hourText.text = "<mspace=" + monospaceSize.ToString() + "px>" + stringText + "</mspace>";
    }

    // Formatea la hora decimal a string
    private string ToHour(float hour)
    {
        // La hora es el entero
        int hours = (int)hour;

        // Los minutos son los decimales, redondeado al minuto mas cercano
        float mins = hour - hours;
        mins = Mathf.Round(mins * 60);


        // Si los minutos llegan a 60 o mas, pasa de hora y los minutos vuelven a 0
        if (mins >= 60)
        {
            hours++;
            mins = 0;
        }

        string formattedHour = "";

        // Si la hora tiene una cifra, se anade un 0 delante
        if (hours < 10)
        {
            formattedHour += "0";
        }
        // Se pone el numero y : al final del string
        formattedHour += hours.ToString() + ":";

        // Si los minutos tienen una cifra, se anade un 0 delante
        if (mins < 10)
        {
            formattedHour += "0";
        }
        // Se pone el numero al final del string
        formattedHour += mins.ToString();

        return formattedHour;
    }

    private void ChangeLight(float timePercent)
    {
        lightSource.color = lightGradient.Evaluate(timePercent);
    }

    public float GetCurrentHour()
    {
        return hour;
    }

    public string GetCurrentFormattedHour()
    {
        return ToHour(hour);
    }

    public void AddNPCBehavior(NPCBehavior tree)
    {
        npcsBehaviors.Add(tree);
    }

    private void LoadLandmarks()
    {
        Regex regExpression = new Regex("[A-Z]", RegexOptions.Compiled);

        MatchEvaluator matchEvaluator = match =>
        {
            string lowerMatch = match.Value.ToLowerInvariant();
            return match.Index > 0 ? $" {lowerMatch}" : lowerMatch;
        };

        landmarksByName = new Dictionary<string, GameObject>();
        if (landmarksParent != null)
        {
            Transform aux = landmarksParent.transform;
            foreach (Transform child in aux)
            {
                string name = regExpression.Replace(child.name, matchEvaluator);
                landmarksByName[name] = child.gameObject;
            }
        }
    }

    public GameObject GetLandmark(string name)
    {
        if (landmarksByName.ContainsKey(name.ToLower()))
        {
            return landmarksByName[name.ToLower()];
        }
        return null;
    }
}
