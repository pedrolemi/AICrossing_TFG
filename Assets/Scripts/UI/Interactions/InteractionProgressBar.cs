using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class InteractionProgressBar : MonoBehaviour
{
    private const float CLOSING_TIME = 0.3f;
    private const float PROGRESS_BEGINNING = 0.0f;
    private const float PROGRESS_END = 1.0f;

    [SerializeField]
    private List<Sprite> sprites;

    private Image image;

    void Start()
    {
        image = GetComponent<Image>();
        gameObject.SetActive(false);
    }

    private IEnumerator FinishProgress()
    {
        yield return new WaitForSeconds(CLOSING_TIME);
        gameObject.SetActive(false);
    }

    public void SetProgress(float progressPercent)
    {
        progressPercent = Mathf.Clamp(progressPercent, PROGRESS_BEGINNING, PROGRESS_END);

        if (progressPercent <= PROGRESS_BEGINNING)
        {
            gameObject.SetActive(true);
        }

        int currentIndex = (int)(progressPercent * (sprites.Count - 1));
        image.sprite = sprites[currentIndex];

        if (progressPercent >= PROGRESS_END)
        {
            StartCoroutine(FinishProgress());
        }
    }
}
