
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class TutorialSlideController : MonoBehaviour
{
    [Header("Tutorial Slides")]
    [SerializeField] private List<GameObject> slides = new List<GameObject>();

    [Header("Navigation Buttons")]
    [SerializeField] private Button nextButton;
    [SerializeField] private Button previousButton;

    private int currentSlideIndex = 0;

    private void Start()
    {
        // Asociar botones
        nextButton.onClick.AddListener(NextSlide);
        previousButton.onClick.AddListener(PreviousSlide);

        // Mostrar primera página
        UpdateSlides();
    }

    public void NextSlide()
    {
        if (slides.Count == 0) return;

        currentSlideIndex++;

        if (currentSlideIndex >= slides.Count)
        {
            currentSlideIndex = 0;
        }

        UpdateSlides();
    }

    public void PreviousSlide()
    {
        if (slides.Count == 0) return;

        currentSlideIndex--;

        if (currentSlideIndex < 0)
        {
            currentSlideIndex = slides.Count - 1;
        }

        UpdateSlides();
    }

    private void UpdateSlides()
    {
        for (int i = 0; i < slides.Count; i++)
        {
            slides[i].SetActive(i == currentSlideIndex);
        }
    }
}
