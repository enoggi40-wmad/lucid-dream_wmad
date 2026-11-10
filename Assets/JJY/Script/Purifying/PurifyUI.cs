using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PurifyUI : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Slider slider;
    [SerializeField] private TMP_Text progressText;

    private void Awake()
    {
        Hide();
    }
    public void Show(float progress01, float requiredTime, float currentTime)
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (slider != null)
            slider.value = Mathf.Clamp01(progress01);

        if (progressText != null)
            progressText.text = $"{currentTime:0.0} / {requiredTime:0.0}s";
    }

    public void Hide()
    {
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (slider != null)
            slider.value = 0f;

        if (progressText != null)
            progressText.text = "";
    }

}