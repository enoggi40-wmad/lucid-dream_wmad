using UnityEngine;

public class WJ_PortalTrigger : MonoBehaviour
{
    private WJ_LucidPortal lucidPortal;

    private void Awake()
    {
        lucidPortal = GetComponentInParent<WJ_LucidPortal>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (lucidPortal == null)
        {
            Debug.LogWarning("[WJ_PortalTrigger] 부모에서 WJ_LucidPortal을 찾지 못했습니다.");
            return;
        }

        lucidPortal.EnterPortal(other);
    }
}