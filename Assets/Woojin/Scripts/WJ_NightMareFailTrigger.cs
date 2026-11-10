using UnityEngine;
using UnityEngine.SceneManagement;

public class WJ_NightmareFailTrigger : MonoBehaviour
{
    [Header("Fail Scene")]
    [SerializeField] private string failSceneName = "WJ_Fail";

    [Header("디버그")]
    [SerializeField] private bool showDebugLog = true;

    private bool hasFailed = false;

    private void OnTriggerEnter(Collider other)
    {
        TryFail(other.transform, "TriggerEnter");
    }

    private void OnTriggerStay(Collider other)
    {
        TryFail(other.transform, "TriggerStay");
    }

    private void OnCollisionEnter(Collision collision)
    {
        TryFail(collision.transform, "CollisionEnter");
    }

    private void OnCollisionStay(Collision collision)
    {
        TryFail(collision.transform, "CollisionStay");
    }

    private void TryFail(Transform otherTransform, string reason)
    {
        if (hasFailed) return;
        if (otherTransform == null) return;

        if (!IsPlayerTransform(otherTransform))
        {
            return;
        }

        hasFailed = true;

        if (showDebugLog)
        {
            Debug.Log("[WJ_NightmareFailTrigger] Player 접촉 감지. Fail 이동. 감지 방식: " + reason);
        }

        if (WJ_GameResultManager.Instance != null)
        {
            WJ_GameResultManager.Instance.LoadFailScene("Nightmare touched Player");
        }
        else
        {
            Debug.LogWarning("[WJ_NightmareFailTrigger] WJ_GameResultManager.Instance가 없습니다. SceneManager로 직접 이동합니다.");
            SceneManager.LoadScene(failSceneName);
        }
    }

    private bool IsPlayerTransform(Transform target)
    {
        Transform current = target;

        while (current != null)
        {
            if (current.CompareTag("Player"))
            {
                return true;
            }

            current = current.parent;
        }

        return false;
    }
}