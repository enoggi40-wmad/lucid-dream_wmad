using System.Collections;
using TMPro;
using UnityEngine;
using LucidDream.Core;
using LucidDream.Integration;

namespace LucidDream.UI
{
    /// <summary>
    /// 인게임 HUD 보조 요소. (정신력 바·해리성 단계 표시는 기존 SanityUI가 담당)
    ///
    ///  - 이상현상 정화 진행 카운터: "정화 2 / 5"
    ///  - 중앙 알림 텍스트: 이상현상 정화 / 해리성 단계 상승 등의 순간 알림을 잠깐 띄웠다 페이드아웃
    /// </summary>
    public class GameHUD : MonoBehaviour
    {
        [Header("이상현상 카운터")]
        [SerializeField] private TMP_Text anomalyCounterText;
        [SerializeField] private string counterFormat = "정화  {0} / {1}";

        [Header("중앙 알림")]
        [SerializeField] private TMP_Text notificationText;
        [SerializeField] private float notificationHoldSeconds = 2.0f;
        [SerializeField] private float notificationFadeSeconds = 1.0f;

        private Coroutine notificationRoutine;

        private void Start()
        {
            if (AnomalyDirector.Instance != null)
            {
                AnomalyDirector.Instance.OnProgressChanged.AddListener(UpdateCounter);
                AnomalyDirector.Instance.OnAnomalyPurified.AddListener(OnAnomalyPurified);
                UpdateCounter(AnomalyDirector.Instance.PurifiedCount, AnomalyDirector.Instance.Total);
            }

            if (DissociationLevelManager.Instance != null)
                DissociationLevelManager.Instance.OnForcedLevelUp.AddListener(OnForcedLevelUp);

            if (notificationText != null)
                SetTextAlpha(notificationText, 0f);
        }

        private void OnDestroy()
        {
            if (AnomalyDirector.Instance != null)
            {
                AnomalyDirector.Instance.OnProgressChanged.RemoveListener(UpdateCounter);
                AnomalyDirector.Instance.OnAnomalyPurified.RemoveListener(OnAnomalyPurified);
            }

            if (DissociationLevelManager.Instance != null)
                DissociationLevelManager.Instance.OnForcedLevelUp.RemoveListener(OnForcedLevelUp);
        }

        private void UpdateCounter(int purified, int total)
        {
            if (anomalyCounterText != null)
                anomalyCounterText.text = string.Format(counterFormat, purified, total);
        }

        private void OnAnomalyPurified(AnomalyPurifiable anomaly)
        {
            ShowNotification("이상현상이 정화되었습니다");
        }

        private void OnForcedLevelUp(DissociationLevelManager.LevelData data)
        {
            ShowNotification($"해리성 단계 상승 — {data.name}");
        }

        /// <summary>중앙에 알림 텍스트를 잠깐 띄운다. 외부 시스템에서도 호출 가능.</summary>
        public void ShowNotification(string message)
        {
            if (notificationText == null) return;

            if (notificationRoutine != null)
                StopCoroutine(notificationRoutine);
            notificationRoutine = StartCoroutine(NotificationRoutine(message));
        }

        private IEnumerator NotificationRoutine(string message)
        {
            notificationText.text = message;
            SetTextAlpha(notificationText, 1f);

            // 일시정지(timeScale=0) 중에도 알림이 멈추지 않도록 unscaled 시간을 쓴다.
            yield return new WaitForSecondsRealtime(notificationHoldSeconds);

            float t = 0f;
            while (t < notificationFadeSeconds)
            {
                t += Time.unscaledDeltaTime;
                SetTextAlpha(notificationText, Mathf.Lerp(1f, 0f, t / notificationFadeSeconds));
                yield return null;
            }
            SetTextAlpha(notificationText, 0f);
        }

        private static void SetTextAlpha(TMP_Text text, float alpha)
        {
            var c = text.color;
            c.a = alpha;
            text.color = c;
        }
    }
}
