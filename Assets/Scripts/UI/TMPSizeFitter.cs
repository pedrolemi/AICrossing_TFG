using UnityEngine;
using UnityEngine.UI;

[ExecuteInEditMode]
public class TMPSizeFitter : MonoBehaviour
{
    RectTransform rectTransform;
    LayoutElement layoutElement;

    private void Start()
    {
        layoutElement = GetComponent<LayoutElement>();
        rectTransform = GetComponent<RectTransform>();
        layoutElement.enabled = false;
    }

    private void OnEnable()
    {
        SetWidth();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        Start();
        SetWidth();
    }
#endif

    private void SetWidth()
    {
        if (rectTransform != null && layoutElement != null)
        {
            if (rectTransform.sizeDelta.x > layoutElement.preferredWidth)
            {
                layoutElement.enabled = true;
            }
        }
    }
}