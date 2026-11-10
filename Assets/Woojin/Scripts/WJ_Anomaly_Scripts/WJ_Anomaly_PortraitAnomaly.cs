using UnityEngine;

public class WJ_Anomaly_PortraitAnomaly : MonoBehaviour
{
    [Header("초상화 Renderer")]
    [SerializeField] private Renderer portraitRenderer;

    [Header("초상화 이미지")]
    [SerializeField] private Texture normalTexture;
    [SerializeField] private Texture lookSideTexture;
    [SerializeField] private Texture smileTexture;

    [Header("변화 시간")]
    [SerializeField] private float firstChangeTime = 10f;
    [SerializeField] private float secondChangeTime = 20f;

    private void Start()
    {
        SetPortrait(normalTexture);
    }

    private void Update()
    {
        float time = Time.timeSinceLevelLoad;

        if (time >= secondChangeTime)
        {
            SetPortrait(smileTexture);
        }
        else if (time >= firstChangeTime)
        {
            SetPortrait(lookSideTexture);
        }
        else
        {
            SetPortrait(normalTexture);
        }
    }

    private void SetPortrait(Texture texture)
    {
        if (portraitRenderer == null || texture == null) return;

        portraitRenderer.material.SetTexture("_BaseMap", texture);
    }
}
