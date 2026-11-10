using UnityEngine;
using LucidDream.Core;

public class WJ_NightmareVisibility : MonoBehaviour
{
    [Header("Renderers")]
    [SerializeField] private Renderer[] nightmareRenderers;

    [Header("Visibility Alpha")]
    [SerializeField] private float hiddenAlpha = 0f;
    [SerializeField] private float lowSanityAlpha = 0.4f;
    [SerializeField] private float breakAlpha = 1f;

    [Header("Sanity Threshold")]
    [SerializeField] private float lowSanityRatio = 0.3f;

    private Material[] nightmareMaterials;

    private void Start()
    {
        if (nightmareRenderers == null || nightmareRenderers.Length == 0)
        {
            nightmareRenderers = GetComponentsInChildren<Renderer>(true);
        }

        if (nightmareRenderers == null || nightmareRenderers.Length == 0)
        {
            Debug.LogWarning("[WJ_NightmareVisibility] Renderer가 없습니다.");
            return;
        }

        CacheMaterials();

        // 시작 상태는 완전 투명
        SetAlpha(hiddenAlpha);

        if (SanityManager.Instance == null)
        {
            Debug.LogWarning("[WJ_NightmareVisibility] SanityManager.Instance가 없습니다.");
            return;
        }

        SanityManager.Instance.OnLowSanityReached.AddListener(ShowLowSanityState);
        SanityManager.Instance.OnSanityBreak.AddListener(ShowSanityBreakState);

        CheckCurrentSanityState();
    }

    private void OnDestroy()
    {
        if (SanityManager.Instance == null) return;

        SanityManager.Instance.OnLowSanityReached.RemoveListener(ShowLowSanityState);
        SanityManager.Instance.OnSanityBreak.RemoveListener(ShowSanityBreakState);
    }

    private void CacheMaterials()
    {
        int materialCount = 0;

        foreach (Renderer renderer in nightmareRenderers)
        {
            if (renderer == null) continue;
            materialCount += renderer.materials.Length;
        }

        nightmareMaterials = new Material[materialCount];

        int index = 0;

        foreach (Renderer renderer in nightmareRenderers)
        {
            if (renderer == null) continue;

            Material[] materials = renderer.materials;

            for (int i = 0; i < materials.Length; i++)
            {
                nightmareMaterials[index] = materials[i];
                index++;
            }
        }
    }

    private void CheckCurrentSanityState()
    {
        if (SanityManager.Instance == null) return;

        float currentSanity = SanityManager.Instance.CurrentSanity;
        float maxSanity = SanityManager.Instance.MaxSanity;

        if (maxSanity <= 0f) return;

        float sanityRatio = currentSanity / maxSanity;

        Debug.Log("[WJ_NightmareVisibility] 현재 Sanity 비율: " + sanityRatio);

        if (currentSanity <= 0f)
        {
            ShowSanityBreakState();
        }
        else if (sanityRatio <= lowSanityRatio)
        {
            ShowLowSanityState();
        }
        else
        {
            HideNightmare();
        }
    }

    private void HideNightmare()
    {
        SetAlpha(hiddenAlpha);
        Debug.Log("[WJ_NightmareVisibility] Nightmare 숨김");
    }

    private void ShowLowSanityState()
    {
        SetAlpha(lowSanityAlpha);
        Debug.Log("[WJ_NightmareVisibility] Sanity 30% 이하. Nightmare 반투명 표시");
    }

    private void ShowSanityBreakState()
    {
        SetAlpha(breakAlpha);
        Debug.Log("[WJ_NightmareVisibility] Sanity 0%. Nightmare 완전 표시");
    }

    private void SetAlpha(float alpha)
    {
        if (nightmareMaterials == null) return;

        foreach (Material material in nightmareMaterials)
        {
            if (material == null) continue;

            Color color = material.color;
            color.a = alpha;
            material.color = color;
        }
    }
}