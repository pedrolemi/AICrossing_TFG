using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "Choice Node", menuName = "Dialog Nodes/Choice Node")]
public class ChoiceNode : DialogNode
{
    [TextArea]
    public List<string> choices = new List<string>();

    public List<UnityEvent> actions = new List<UnityEvent>();
}
