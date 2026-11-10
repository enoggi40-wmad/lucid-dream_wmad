using UnityEngine;
using LucidDream.Core;

namespace LucidDream.Integration
{
    /// <summary>
    /// UI 패널(예: 꿈 판별 패널 dreamQuestionPanel)에 붙이는 드롭인 컴포넌트.
    /// 패널이 활성화되어 있는 동안 정신력이 떨어지지 않게 잠그고, 비활성화되면 풀어준다.
    ///
    /// 패널을 SetActive(true/false)로 토글하는 구조면 OnEnable/OnDisable이 정확히
    /// "UI 뜰 때 / 닫힐 때"에 맞춰 호출되므로, 코드 수정 없이 컴포넌트만 붙이면 끝난다.
    /// (SanityManager의 freeze/pause는 참조 카운트라 여러 UI가 겹쳐도 안전하다.)
    /// </summary>
    public class SanityFreezeBinding : MonoBehaviour
    {
        public enum Mode
        {
            FreezeAll,   // 모든 감소 차단 (자연 감소 + 이상현상 피해). "감소하지 않게"에 해당. 기본값.
            PassiveOnly, // 자연 감소(Passive Decay)만 멈춤. 이상현상 피해는 그대로 들어간다.
        }

        [SerializeField] private Mode mode = Mode.FreezeAll;

        [Tooltip("이 오브젝트가 활성화되면 자동으로 잠그고, 비활성화되면 해제한다. (SetActive 토글용)")]
        [SerializeField] private bool bindToEnableState = true;

        private bool applied = false;

        private void OnEnable()
        {
            if (bindToEnableState) Apply();
        }

        private void OnDisable()
        {
            if (bindToEnableState) Release();
        }

        /// <summary>정신력 잠금 적용. UI를 SetActive로 토글하지 않는 경우 직접 호출.</summary>
        public void Apply()
        {
            if (applied) return;
            var s = SanityManager.Instance;
            if (s == null) return;

            if (mode == Mode.FreezeAll) s.FreezeSanity();
            else s.PausePassiveDecay();
            applied = true;
        }

        /// <summary>정신력 잠금 해제. Apply와 1:1로 짝이 맞는다(참조 카운트).</summary>
        public void Release()
        {
            if (!applied) return;
            var s = SanityManager.Instance;
            if (s != null)
            {
                if (mode == Mode.FreezeAll) s.UnfreezeSanity();
                else s.ResumePassiveDecay();
            }
            applied = false;
        }
    }
}
