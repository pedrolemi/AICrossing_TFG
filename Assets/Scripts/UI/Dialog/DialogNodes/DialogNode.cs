using System;
using System.Collections.Generic;
using UnityEngine;

// - Un scriptable es un asset. De modo que cada uno que se crea es unico y si hay varias
// clases haciendo referencia a el, haran referencia a la misma instancia
// - Se puede saber que el nodo siguiente es de un tipo u otro comprobando el tipo 
public abstract class DialogNode : ScriptableObject
{
    [SerializeField]
    private List<DialogNode> baseNextNodes = new List<DialogNode>();

    [NonSerialized]
    private List<DialogNode> nextNodes = new List<DialogNode>();

    [NonSerialized]
    private HashSet<DialogNode> runtimeNodes = new HashSet<DialogNode>();

    private void OnEnable()
    {
        nextNodes = baseNextNodes;
    }

    public void AddNextNode(DialogNode node)
    {
        if (!runtimeNodes.Contains(node))
        {
            runtimeNodes.Add(node);
            nextNodes.Add(node);
        }
    }

    public DialogNode GetNextNodeByIndex(int index)
    {
        DialogNode node = nextNodes[index];
        if (runtimeNodes.Contains(node))
        {
            runtimeNodes.Remove(node);
            nextNodes.RemoveAt(index);
        }
        return node;
    }

    public int GetNextNodesLength()
    {
        return nextNodes.Count;
    }
}
