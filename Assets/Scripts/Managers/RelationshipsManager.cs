using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class RelationshipsManager : Singleton<RelationshipsManager>
{
    class FriendshipAndScrollItem
    {
        public FriendshipAndScrollItem(int lv, RelationshipListItem item)
        {
            level = lv;
            scrollItem = item;
        }
        public int level { get; set; }
        public RelationshipListItem scrollItem { get; set; }
    }

    [SerializeField]
    [Range(0, 10)]
    int DEFAULT_LEVEL = 5;

    [SerializeField]
    GameObject contentsObj;     // Objeto padre que contiene los elementos de la listView

    [SerializeField]
    GameObject itemPrefab;      // Prefab de cada elemento de la listView

    [SerializeField]
    private int questionMinPoints = -2;
    public int QuestionMinPoints => questionMinPoints;

    [SerializeField]
    int questionMaxPoints = 2;
    public int QuestionMaxPoints => questionMaxPoints;

    [SerializeField]
    int minFriendshipPoints = 0;
    [SerializeField]
    int maxFriendshipPoints = 10;

    Dictionary<string, FriendshipAndScrollItem> npcsFriendship;

    EventSystem eventSystem;

    protected override void Awake()
    {
        base.Awake();
        eventSystem = EventSystem.current;
        npcsFriendship = new Dictionary<string, FriendshipAndScrollItem>();
    }

    // Crea y anade la relacion con el npc indicado
    private RelationshipListItem AddScrollViewElement(string npcName, int level)
    {
        GameObject elem = Instantiate(itemPrefab, contentsObj.transform);

        // Se actualizan los textos del objeto
        RelationshipListItem it = elem.GetComponent<RelationshipListItem>();
        it.CreateElement(npcName, level);

        elem.GetComponent<Button>().onClick.AddListener(() =>
        {
            eventSystem.SetSelectedGameObject(null);
        });

        return it;
    }

    public void UpdateFriendship(string npc, int amount)
    {
        if (!npcsFriendship.ContainsKey(npc))
        {
            npcsFriendship.Add(npc, new FriendshipAndScrollItem(DEFAULT_LEVEL, AddScrollViewElement(npc, DEFAULT_LEVEL)));
        }
        npcsFriendship[npc].level += amount;
        npcsFriendship[npc].level = Mathf.Clamp(npcsFriendship[npc].level, minFriendshipPoints, maxFriendshipPoints);
        npcsFriendship[npc].scrollItem.UpdateRelationship(npcsFriendship[npc].level);
    }

    public int GetFriendship(string npc)
    {
        return npcsFriendship.ContainsKey(npc) ? npcsFriendship[npc].level : 0;
    }

    public float GetAverageFriendship()
    {
        float avgFriendship = 0.0f;
        foreach (FriendshipAndScrollItem it in npcsFriendship.Values)
        {
            avgFriendship += it.level;
        }
        avgFriendship /= npcsFriendship.Count;

        return avgFriendship;
    }
}
