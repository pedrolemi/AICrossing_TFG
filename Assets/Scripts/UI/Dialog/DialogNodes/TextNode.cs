using UnityEngine;

[CreateAssetMenu(fileName = "Text Node", menuName = "Dialog Nodes/Text Node")]
public class TextNode : DialogNode
{
    public string characterName;

    [TextArea(5, 10)]
    public string text;

    public bool canAnswer = true;
}
