using UnityEngine;

public class BeamMaterialScroller : MonoBehaviour
{
    private static readonly AnimationCurve FlatWidthCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private float scrollSpeedX = 1.5f;
    [SerializeField] private float scrollSpeedY = 0f;
    [SerializeField] private float pulseSpeed = 4f;
    [SerializeField] private float minAlpha = 0.18f;
    [SerializeField] private float maxAlpha = 0.34f;
    [SerializeField] private float minWidth = 0.024f;
    [SerializeField] private float maxWidth = 0.036f;

    private Material runtimeMaterial;
    private Color baseColor;

    private void Awake()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        if (lineRenderer != null && lineRenderer.material != null)
        {
            ApplyFlatWidth(minWidth);
            runtimeMaterial = lineRenderer.material;
            baseColor = runtimeMaterial.color;
        }
    }

    private void Update()
    {
        if (runtimeMaterial == null)
            return;

        float offsetX = Time.time * scrollSpeedX;
        float offsetY = Time.time * scrollSpeedY;
        runtimeMaterial.mainTextureOffset = new Vector2(offsetX, offsetY);
        float width = Mathf.Lerp(minWidth, maxWidth, (Mathf.Sin(Time.time * 6f) + 1f) * 0.5f);
        ApplyFlatWidth(width);
        float t = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
        float alpha = Mathf.Lerp(minAlpha, maxAlpha, t);

        Color c = baseColor;
        c.a = alpha;
        runtimeMaterial.color = c;
    }

    private void ApplyFlatWidth(float width)
    {
        if (lineRenderer == null)
            return;

        lineRenderer.widthMultiplier = width;
        lineRenderer.widthCurve = FlatWidthCurve;
        lineRenderer.startWidth = width;
        lineRenderer.endWidth = width;
    }
}
