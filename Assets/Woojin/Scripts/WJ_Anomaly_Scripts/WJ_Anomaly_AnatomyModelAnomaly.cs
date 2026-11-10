using UnityEngine;

public class WJ_Anomaly_AnatomyModelAnomaly : MonoBehaviour
{
    [Header("플레이어 카메라")]
    [SerializeField] private Camera playerCamera;

    [Header("순간이동 위치들")]
    [SerializeField] private Transform[] teleportPoints;

    [Header("순간이동 설정")]
    [SerializeField] private float teleportCooldown = 3f;

    private float cooldownTimer = 0f;

    private void Update()
    {
        if (playerCamera == null) return;
        if (teleportPoints == null || teleportPoints.Length == 0) return;

        cooldownTimer += Time.deltaTime;

        bool isVisible = IsVisibleFromCamera();

        if (isVisible)
        {
            return;
        }

        if (cooldownTimer >= teleportCooldown)
        {
            TeleportToRandomPoint();
            cooldownTimer = 0f;
        }
    }

    private bool IsVisibleFromCamera()
    {
        Vector3 viewPos = playerCamera.WorldToViewportPoint(transform.position);

        bool inView =
            viewPos.z > 0 &&
            viewPos.x > 0 && viewPos.x < 1 &&
            viewPos.y > 0 && viewPos.y < 1;

        return inView;
    }

    private void TeleportToRandomPoint()
    {
        int randomIndex = Random.Range(0, teleportPoints.Length);

        transform.position = teleportPoints[randomIndex].position;
        transform.rotation = teleportPoints[randomIndex].rotation;

        Debug.Log("[WJ_AnatomyModelTeleportAnomaly] 인체 모형 순간이동");
    }
}