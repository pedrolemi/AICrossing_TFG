using LLM;
using System.Collections.Generic;
using UnityEngine;

public class NPCTriggerManager : Singleton<NPCTriggerManager>
{
    LLMProvider provider;
    Personality personality;
    GameObject npcObj;
    HashSet<string> metNpcs;

    [SerializeField]
    string defaultLayerName;
    [SerializeField]
    string outlineLayerName;


    void Start()
    {
        personality = null;
        provider = null;
        npcObj = null;
        metNpcs = new HashSet<string>();
    }

    // Al entrar en algun trigger, si no se estaba colisionando con ningun npc y el
    // objeto que tiene el trigger es un npc (tiene un LLMProvider), se guardan
    // su provider, el objeto con el que se ha colisionado, y el nombre del provider
    void OnTriggerEnter2D(Collider2D collision)
    {
        if (provider == null && personality == null)
        {
            provider = collision.gameObject.GetComponent<LLMProvider>();
            personality = collision.gameObject.GetComponent<Personality>();

            if (provider != null && personality != null)
            {
                npcObj = collision.gameObject;
                SetOutline(true);
            }
        }

    }

    // Al salir de algun trigger, si se estaba colisionando con algun npc, se dejan de
    // guardar su provider, el objeto con el que se ha colisionado y el nombre del provider
    void OnTriggerExit2D(Collider2D collision)
    {
        if (provider != null && personality != null)
        {
            SetOutline(false);
            provider = null;
            personality = null;
            npcObj = null;
        }
    }


    // Devuelve el objeto del npc mas cercano
    public GameObject GetNPC() { return npcObj; }

    public LLMProvider GetNPCProvider() { return provider; }
    public Personality GetPersonality() { return personality; }

    public void SetTalked(string npc) { metNpcs.Add(npc); }
    public bool GetTalked(string npc) { return metNpcs.Contains(npc); }


    private void SetOutline(bool outlined)
    {
        if (npcObj != null)
        {
            CharacterAnimationController animController = npcObj.GetComponent<CharacterAnimationController>();
            if (animController != null)
            {
                if (!outlined)
                {
                    animController.SetSpritesLayer(defaultLayerName);
                }
                else
                {
                    animController.SetSpritesLayer(outlineLayerName);
                }
            }
        }
    }
}
