using LucidDream.Core;
using UnityEngine;

public class PurifyController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera playerCamera;
    [SerializeField] private Animator animator;
    [SerializeField] private PurifyUI purifyUI;
    [SerializeField] private PurifyBeamController beamController;

    [Header("Detection")]
    [SerializeField] private float maxDistance = 6f;
    [SerializeField] private LayerMask targetLayerMask = ~0;
    [SerializeField] private float lineOfSightOffset = 0.1f;

    [Header("Input")]
    [SerializeField] private int purifyMouseButton = 1; // Right Mouse Button

    [Header("Sanity")]
    [SerializeField] private bool requireSanityToPurify = true;
    [SerializeField, Min(0f)] private float minimumSanityToPurify = 0.01f;

    private static readonly int IsPurifyingHash = Animator.StringToHash("IsPurifying");

    private PurifiableObject currentTarget;

    public bool IsPurifying { get; private set; }
    public float MaxDistance => maxDistance;

    private void Awake()
    {
        if (playerCamera == null && Camera.main != null)
            playerCamera = Camera.main;
    }

    private void Update()
    {
        HandlePurification();
    }

    private void HandlePurification()
    {
        bool isHoldingPurify = Input.GetMouseButton(purifyMouseButton);

        if (!isHoldingPurify)
        {
            StopPurification();
            return;
        }

        if (!CanPurifyWithCurrentSanity())
        {
            StopPurification();
            return;
        }

        PurifiableObject detectedTarget = FindPurifyTarget();

        if (detectedTarget == null)
        {
            StopPurification();
            return;
        }

        if (!detectedTarget.CanBePurified)
        {
            StopPurification();
            return;
        }

        if (!IsTargetVisible(detectedTarget))
        {
            StopPurification();
            return;
        }

        if (!IsWithinDistance(detectedTarget))
        {
            StopPurification();
            return;
        }

        if (currentTarget != detectedTarget)
        {
            ChangeTarget(detectedTarget);
        }

        KeepPurifyingCurrentTarget();
    }

    private PurifiableObject FindPurifyTarget()
    {
        if (playerCamera == null)
            return null;

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, maxDistance, targetLayerMask, QueryTriggerInteraction.Ignore))
        {
            return hit.collider.GetComponentInParent<PurifiableObject>();
        }

        return null;
    }

    private bool IsWithinDistance(PurifiableObject target)
    {
        if (target == null || playerCamera == null)
            return false;

        float distance = Vector3.Distance(playerCamera.transform.position, target.EffectPoint.position);
        return distance <= maxDistance;
    }

    private bool IsTargetVisible(PurifiableObject target)
    {
        if (target == null || playerCamera == null)
            return false;

        Vector3 origin = playerCamera.transform.position;
        Vector3 destination = target.EffectPoint.position + Vector3.up * lineOfSightOffset;
        Vector3 direction = destination - origin;
        float distance = direction.magnitude;

        if (Physics.Raycast(origin, direction.normalized, out RaycastHit hit, distance, ~0, QueryTriggerInteraction.Ignore))
        {
            PurifiableObject hitTarget = hit.collider.GetComponentInParent<PurifiableObject>();
            return hitTarget == target;
        }

        return true;
    }

    private void ChangeTarget(PurifiableObject newTarget)
    {
        if (currentTarget != null)
        {
            currentTarget.OnPurifyStop();
        }

        if (beamController != null)
        {
            beamController.StopBeam();
        }

        currentTarget = newTarget;

        if (currentTarget != null)
        {
            currentTarget.OnPurifyStart();

            if (beamController != null)
            {
                beamController.StartBeam(currentTarget.EffectPoint);
            }
        }
    }

    private void KeepPurifyingCurrentTarget()
    {
        if (currentTarget == null)
            return;

        if (!CanPurifyWithCurrentSanity())
        {
            StopPurification();
            return;
        }

        IsPurifying = true;

        if (animator != null)
        {
            animator.SetBool(IsPurifyingHash, true);
        }

        currentTarget.AddPurifyTime(Time.deltaTime);

        if (purifyUI != null)
        {
            purifyUI.Show(
                currentTarget.Progress01,
                currentTarget.RequiredPurifyTime,
                currentTarget.CurrentPurifyTime
            );
        }

        if (currentTarget.Progress01 >= 1f)
        {
            currentTarget.CompletePurification();

            if (purifyUI != null)
                purifyUI.Hide();

            IsPurifying = false;

            if (animator != null)
                animator.SetBool(IsPurifyingHash, false);

            currentTarget.OnPurifyStop();

            if (beamController != null)
                beamController.StopBeam();

            currentTarget = null;
        }
    }

    private bool CanPurifyWithCurrentSanity()
    {
        if (!requireSanityToPurify)
            return true;

        SanityManager sanityManager = SanityManager.Instance;

        if (sanityManager == null)
            return true;

        return !sanityManager.IsBroken && sanityManager.CurrentSanity > minimumSanityToPurify;
    }

    private void StopPurification()
    {
        if (currentTarget != null)
        {
            currentTarget.OnPurifyStop();
        }

        currentTarget = null;
        IsPurifying = false;

        if (animator != null)
            animator.SetBool(IsPurifyingHash, false);

        if (purifyUI != null)
            purifyUI.Hide();

        if (beamController != null)
            beamController.StopBeam();
    }
}
