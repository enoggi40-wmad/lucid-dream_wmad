using UnityEngine;

public class PurifyBeamController : MonoBehaviour
{
    private static readonly AnimationCurve FlatWidthCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

    [Header("References")]
    [SerializeField] private Transform handSocket;
    [SerializeField] private LineRenderer lineRenderer;

    [Header("Optional Effects")]
    [SerializeField] private ParticleSystem handLoopEffect;
    [SerializeField] private ParticleSystem hitEffect;

    [Header("Beam Settings")]
    [SerializeField] private float startWidth = 0.03f;
    [SerializeField] private float endWidth = 0.03f;

    [Header("Wave Motion")]
    [SerializeField, Min(2)] private int segmentCount = 18;
    [SerializeField, Min(0f)] private float waveAmplitude = 0.08f;
    [SerializeField, Min(0f)] private float waveFrequency = 3f;
    [SerializeField] private float waveSpeed = 8f;
    [SerializeField, Min(0f)] private float secondaryWaveAmplitude = 0.035f;
    [SerializeField, Min(0f)] private float secondaryWaveFrequency = 5f;

    private Transform currentTargetPoint;
    private bool isPlaying;

    private void Awake()
    {
        ConfigureLineRenderer();
        StopBeamImmediate();
    }

    private void LateUpdate()
    {
        if (!isPlaying || handSocket == null || currentTargetPoint == null || lineRenderer == null)
            return;

        UpdateBeamPositions();

        if (hitEffect != null)
        {
            hitEffect.transform.position = currentTargetPoint.position;
        }
    }

    public void StartBeam(Transform targetPoint)
    {
        if (targetPoint == null)
            return;

        currentTargetPoint = targetPoint;
        isPlaying = true;

        if (lineRenderer != null)
        {
            ConfigureLineRenderer();
            lineRenderer.enabled = true;
            lineRenderer.positionCount = Mathf.Max(2, segmentCount);
            lineRenderer.startWidth = startWidth;
            lineRenderer.endWidth = endWidth;
            UpdateBeamPositions();
        }

        if (handLoopEffect != null)
        {
            handLoopEffect.gameObject.SetActive(true);
            handLoopEffect.Play(true);
        }

        if (hitEffect != null)
        {
            hitEffect.gameObject.SetActive(true);
            hitEffect.transform.position = targetPoint.position;
            hitEffect.Play(true);
        }
    }

    public void StopBeam()
    {
        isPlaying = false;
        currentTargetPoint = null;

        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
            lineRenderer.positionCount = 0;
        }

        if (handLoopEffect != null)
        {
            handLoopEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            handLoopEffect.gameObject.SetActive(false);
        }

        if (hitEffect != null)
        {
            hitEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            hitEffect.gameObject.SetActive(false);
        }
    }

    private void StopBeamImmediate()
    {
        isPlaying = false;
        currentTargetPoint = null;

        if (lineRenderer != null)
        {
            lineRenderer.enabled = false;
            lineRenderer.positionCount = 0;
        }

        if (handLoopEffect != null)
        {
            handLoopEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            handLoopEffect.gameObject.SetActive(false);
        }

        if (hitEffect != null)
        {
            hitEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            hitEffect.gameObject.SetActive(false);
        }
    }

    private void ConfigureLineRenderer()
    {
        if (lineRenderer == null)
            return;

        lineRenderer.alignment = LineAlignment.View;
        lineRenderer.textureMode = LineTextureMode.Tile;
        ApplyFlatWidth(startWidth);
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

    private void UpdateBeamPositions()
    {
        if (lineRenderer == null || currentTargetPoint == null)
            return;

        Vector3 startPosition = handSocket != null ? handSocket.position : transform.position;
        Vector3 endPosition = currentTargetPoint.position;
        Vector3 beamDirection = endPosition - startPosition;
        int count = Mathf.Max(2, segmentCount);

        lineRenderer.positionCount = count;

        if (beamDirection.sqrMagnitude <= Mathf.Epsilon)
        {
            for (int i = 0; i < count; i++)
                lineRenderer.SetPosition(i, startPosition);

            return;
        }

        Vector3 direction = beamDirection.normalized;
        float beamLength = beamDirection.magnitude;
        float effectiveWaveAmplitude = Mathf.Min(waveAmplitude, beamLength * 0.12f);
        float effectiveSecondaryWaveAmplitude = Mathf.Min(secondaryWaveAmplitude, beamLength * 0.08f);
        Camera mainCamera = Camera.main;
        Vector3 sideAxis = mainCamera != null ? Vector3.Cross(direction, mainCamera.transform.forward) : Vector3.Cross(direction, Vector3.up);

        if (sideAxis.sqrMagnitude <= 0.0001f)
            sideAxis = Vector3.Cross(direction, Vector3.right);

        sideAxis.Normalize();
        Vector3 upAxis = Vector3.Cross(sideAxis, direction).normalized;
        float time = Time.time;

        for (int i = 0; i < count; i++)
        {
            float t = count <= 1 ? 0f : (float)i / (count - 1);
            Vector3 position = Vector3.Lerp(startPosition, endPosition, t);

            if (i > 0 && i < count - 1)
            {
                float endpointFade = Mathf.Sin(t * Mathf.PI);
                float primaryPhase = (t * waveFrequency * Mathf.PI * 2f) - (time * waveSpeed);
                float secondaryPhase = (t * secondaryWaveFrequency * Mathf.PI * 2f) + (time * waveSpeed * 0.7f);
                Vector3 waveOffset =
                    sideAxis * (Mathf.Sin(primaryPhase) * effectiveWaveAmplitude) +
                    upAxis * (Mathf.Cos(secondaryPhase) * effectiveSecondaryWaveAmplitude);

                position += waveOffset * endpointFade;
            }

            lineRenderer.SetPosition(i, position);
        }
    }
}
