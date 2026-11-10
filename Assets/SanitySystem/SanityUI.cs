using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace LucidDream.UI
{
    /// <summary>
    /// 정신력 / 해리성 단계 HUD.
    /// - Sanity 바: 30% 이하일 때 빨갛게 깜빡이는 연출
    /// - 해리성 단계 텍스트: 단계가 오를 때 펄스 애니메이션
    /// - 화면 비네트(사각틀 어두워짐)로 심리적 압박감 표현
    /// </summary>
    public class SanityUI : MonoBehaviour
    {
        [Header("Sanity Bar")]
        [SerializeField] private Image sanityFill;
        [SerializeField] private Gradient sanityColorGradient;
        [SerializeField] private CanvasGroup sanityBarGroup;

        [Header("Dissociation Level")]
        [SerializeField] private TMP_Text levelNameText;
        [SerializeField] private TMP_Text levelNumberText;

        [Header("Vignette")]
        [SerializeField] private Image vignetteImage;
        [SerializeField] private float vignetteMaxAlpha = 0.75f;

        [Header("Low Sanity Pulse")]
        [SerializeField] private float pulseSpeed = 4f;

        private bool pulseActive = false;

        private void OnEnable()
        {
            if (Core.SanityManager.Instance != null)
            {
                Core.SanityManager.Instance.OnSanityChanged.AddListener(UpdateSanityBar);
                Core.SanityManager.Instance.OnLowSanityReached.AddListener(() => pulseActive = true);
                Core.SanityManager.Instance.OnSanityRestored.AddListener(() => pulseActive = false);
            }
            if (Core.DissociationLevelManager.Instance != null)
            {
                Core.DissociationLevelManager.Instance.OnLevelChanged.AddListener(UpdateLevelUI);
            }
        }

        private void Update()
        {
            if (pulseActive && sanityBarGroup != null)
            {
                sanityBarGroup.alpha = 0.5f + 0.5f * Mathf.Abs(Mathf.Sin(Time.time * pulseSpeed));
            }
            else if (sanityBarGroup != null)
            {
                sanityBarGroup.alpha = 1f;
            }
        }

        private void UpdateSanityBar(float current, float max)
        {
            float ratio = current / max;
            if (sanityFill) sanityFill.fillAmount = ratio;
            if (sanityFill && sanityColorGradient != null)
                sanityFill.color = sanityColorGradient.Evaluate(ratio);

            if (vignetteImage)
            {
                float a = Mathf.Lerp(vignetteMaxAlpha, 0f, ratio);
                var c = vignetteImage.color;
                c.a = a;
                vignetteImage.color = c;
            }
        }

        private void UpdateLevelUI(Core.DissociationLevelManager.LevelData data)
        {
            if (levelNameText) levelNameText.text = data.name;
            if (levelNumberText) levelNumberText.text = $"Lv.{data.level}";
        }
    }
}