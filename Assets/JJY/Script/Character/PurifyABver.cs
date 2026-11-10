using UnityEngine;

public class PurifyABver : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private PurifyController purifyController;

    [Header("Legacy Crosshair")]
    [SerializeField] private GameObject worldCrosshair;

    [Header("Highlight")]
    [SerializeField] private float maxDistance = 6f;
    [SerializeField] private LayerMask targetLayerMask = ~0;

    private HighlightObject currentHighlight;

    private void Awake()
    {
        if (playerCamera == null && Camera.main != null)
            playerCamera = Camera.main;

        if (purifyController == null)
            purifyController = GetComponent<PurifyController>();

        HideLegacyCrosshair();
        ClearHighlight();
    }

    private void Update()
    {
        HideLegacyCrosshair();

        if (playerCamera == null)
            return;

        float effectiveMaxDistance = purifyController != null ? purifyController.MaxDistance : maxDistance;
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, effectiveMaxDistance, targetLayerMask, QueryTriggerInteraction.Ignore))
        {
            PurifiableObject purifiable = hit.collider.GetComponentInParent<PurifiableObject>();
            HighlightObject target = ResolveHighlightTarget(hit.collider, purifiable);

            if (target != null && (purifiable == null || (purifiable.CanBePurified && IsWithinPurifyDistance(purifiable, effectiveMaxDistance))))
            {
                if (target == currentHighlight)
                    return;

                ClearHighlight();

                currentHighlight = target;

                if (currentHighlight != null)
                    currentHighlight.SetHighlighted(true);

                return;
            }
        }

        ClearHighlight();
    }

    private bool IsWithinPurifyDistance(PurifiableObject purifiable, float effectiveMaxDistance)
    {
        if (purifiable == null || playerCamera == null)
            return false;

        return Vector3.Distance(playerCamera.transform.position, purifiable.EffectPoint.position) <= effectiveMaxDistance;
    }

    private HighlightObject ResolveHighlightTarget(Collider hitCollider, PurifiableObject purifiable)
    {
        if (purifiable != null)
        {
            HighlightObject purifiableHighlight = purifiable.GetComponent<HighlightObject>();

            if (purifiableHighlight != null)
                return purifiableHighlight;
        }

        HighlightObject hitHighlight = hitCollider.GetComponentInParent<HighlightObject>();

        if (hitHighlight != null)
            return hitHighlight;

        return purifiable != null ? purifiable.GetComponentInChildren<HighlightObject>(true) : null;
    }

    private void HideLegacyCrosshair()
    {
        if (worldCrosshair != null)
            worldCrosshair.SetActive(false);
    }

    private void ClearHighlight()
    {
        if (currentHighlight != null)
        {
            currentHighlight.SetHighlighted(false);
            currentHighlight = null;
        }
    }
}
