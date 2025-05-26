using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class RelationshipListItem : MonoBehaviour
{
    [SerializeField]
    TextMeshProUGUI npcName;

    [SerializeField]
    RawImage[] hearts;

    [SerializeField]
    Color reachedColor;

    [SerializeField]
    Color unreachedColor;

    public void CreateElement(string npc, int level = 0)
    {
        npcName.text = npc;
        UpdateRelationship(level);
    }

    // Actualiza el texto
    public void UpdateRelationship(int level)
    {
        for (int i = 0; i < hearts.Length; i++)
        {
            if (i < level)
            {
                hearts[i].color = reachedColor;
            }
            else
            {
                hearts[i].color = unreachedColor;
            }
        }
    }

}