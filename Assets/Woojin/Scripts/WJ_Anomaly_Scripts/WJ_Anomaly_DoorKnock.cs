using UnityEngine;

public class WJ_Anomaly_DoorKnock : MonoBehaviour
{
    [Header("노크 사운드")]
    [SerializeField] private AudioSource knockAudio;

    [Header("중복 방지")]
    [SerializeField] private float knockCooldown = 0.5f;

    private float lastKnockTime = -999f;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player")) return;

        if (Time.time - lastKnockTime < knockCooldown) return;

        PlayKnock();
        lastKnockTime = Time.time;
    }

    private void PlayKnock()
    {
        Debug.Log("[WJ_Anomaly_DoorKnock] 문에서 노크 소리가 들림");

        if (knockAudio == null)
        {
            Debug.LogWarning("[WJ_Anomaly_DoorKnock] knockAudio가 연결되지 않았습니다.");
            return;
        }

        if (knockAudio.clip == null)
        {
            Debug.LogWarning("[WJ_Anomaly_DoorKnock] AudioSource에 AudioClip이 없습니다.");
            return;
        }

        knockAudio.PlayOneShot(knockAudio.clip);
    }
}