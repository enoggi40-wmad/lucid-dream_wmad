using UnityEngine;
using System.Reflection;

public class FootprintPurifiable : PurifiableObject
{
    private float destroyDelay = 0.15f;
    private static readonly FieldInfo RequiredPurifyTimeField = typeof(PurifiableObject).GetField("requiredPurifyTime", BindingFlags.Instance | BindingFlags.NonPublic);
    private static readonly FieldInfo EffectPointField = typeof(PurifiableObject).GetField("effectPoint", BindingFlags.Instance | BindingFlags.NonPublic);

    public void Initialize(float lifetime, float requiredPurifyTime = 4f, Transform effectPoint = null)
    {
        SetRequiredPurifyTime(requiredPurifyTime);
        SetEffectPoint(effectPoint != null ? effectPoint : transform);

        if (lifetime > 0f)
            Destroy(gameObject, lifetime);
    }

    protected override void OnPurifyProgressChanged(float progress01)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        float alpha = 1f - Mathf.Clamp01(progress01);

        for (int i = 0; i < renderers.Length; i++)
        {
            Material material = renderers[i].material;

            if (material != null && material.HasProperty("_Color"))
            {
                Color color = material.color;
                color.a = alpha;
                material.color = color;
            }
        }
    }

    protected override void OnPurifyComplete()
    {
        base.OnPurifyComplete();
        ClearGuardAndFootprints();
    }

    private void SetRequiredPurifyTime(float requiredPurifyTime)
    {
        if (RequiredPurifyTimeField != null)
            RequiredPurifyTimeField.SetValue(this, Mathf.Max(0.01f, requiredPurifyTime));
    }

    private void SetEffectPoint(Transform effectPoint)
    {
        if (EffectPointField != null)
            EffectPointField.SetValue(this, effectPoint);
    }

    private void ClearGuardAndFootprints()
    {
        GuardPatrolEntity[] guards = Object.FindObjectsByType<GuardPatrolEntity>(FindObjectsSortMode.None);

        for (int i = 0; i < guards.Length; i++)
        {
            if (guards[i] == null)
                continue;

            if (WJ_AnomalyManager.Instance != null)
                WJ_AnomalyManager.Instance.RemoveAnomaly(guards[i].gameObject);

            guards[i].VanishGuard();
        }

        WanderingEntity[] wanderingAdapters = Object.FindObjectsByType<WanderingEntity>(FindObjectsSortMode.None);

        for (int i = 0; i < wanderingAdapters.Length; i++)
        {
            if (wanderingAdapters[i] != null)
                Destroy(wanderingAdapters[i].gameObject, destroyDelay);
        }

        FootprintPurifiable[] footprints = Object.FindObjectsByType<FootprintPurifiable>(FindObjectsSortMode.None);

        for (int i = 0; i < footprints.Length; i++)
        {
            if (footprints[i] != null)
                Destroy(footprints[i].gameObject, destroyDelay);
        }
    }
}
