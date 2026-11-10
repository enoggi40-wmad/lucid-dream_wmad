using UnityEngine;
using UnityEngine.UI;
using LucidDream.Core;

public class WJ_SanityBarUI : MonoBehaviour
{
    [Header("Sanity Slider")]
    [SerializeField] private Slider sanitySlider;

    private void Start()
    {
        if (sanitySlider == null)
        {
            Debug.LogWarning("[WJ_SanityBarUI] sanitySlider가 연결되지 않았습니다.");
            return;
        }

        if (SanityManager.Instance == null)
        {
            Debug.LogWarning("[WJ_SanityBarUI] SanityManager.Instance가 없습니다.");
            return;
        }

        sanitySlider.minValue = 0f;
        sanitySlider.maxValue = 1f;
        sanitySlider.interactable = false;

        SanityManager.Instance.OnSanityChanged.AddListener(UpdateSanityBar);

        UpdateSanityBar(
            SanityManager.Instance.CurrentSanity,
            SanityManager.Instance.MaxSanity
        );
    }

    private void OnDestroy()
    {
        if (SanityManager.Instance == null) return;

        SanityManager.Instance.OnSanityChanged.RemoveListener(UpdateSanityBar);
    }

    private void UpdateSanityBar(float current, float max)
    {
        if (sanitySlider == null) return;

        float ratio = max <= 0f ? 0f : current / max;
        sanitySlider.value = Mathf.Clamp01(ratio);
    }
}