using UnityEngine;
using LucidDream.Core;

public class WJ_NightmareChase : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform player;

    [Header("Chase Settings")]
    [SerializeField] private float chaseSpeed = 3f;
    [SerializeField] private float stopDistance = 1.5f;

    private bool isChasing = false;

    private void Start()
    {
        if (SanityManager.Instance == null)
        {
            Debug.LogWarning("[WJ_NightmareChase] SanityManager.Instance가 없습니다.");
            return;
        }

        SanityManager.Instance.OnSanityBreak.AddListener(StartChase);

        CheckCurrentSanityState();
    }

    private void OnDestroy()
    {
        if (SanityManager.Instance == null) return;

        SanityManager.Instance.OnSanityBreak.RemoveListener(StartChase);
    }

    private void Update()
    {
        if (!isChasing) return;
        if (player == null) return;

        ChasePlayer();
    }

    private void CheckCurrentSanityState()
    {
        if (SanityManager.Instance == null) return;

        float currentSanity = SanityManager.Instance.CurrentSanity;

        if (currentSanity <= 0f)
        {
            StartChase();
        }
    }

    private void StartChase()
    {
        if (isChasing) return;

        isChasing = true;

        Debug.Log("[WJ_NightmareChase] Sanity 0%. Nightmare 추격 시작");
    }

    private void ChasePlayer()
    {
        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= stopDistance) return;

        Vector3 direction = (player.position - transform.position).normalized;
        direction.y = 0f;

        transform.position += direction * chaseSpeed * Time.deltaTime;

        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }
}