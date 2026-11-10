using UnityEngine;
using LucidDream.Core;

namespace LucidDream.Integration
{
    /// <summary>
    /// 이상현상: 거꾸로 걸린 시계.
    /// 등장(활성) 시 시계 몸체가 180도 뒤집히고 바늘이 거꾸로 돈다.
    /// 잠복/정화로 비활성화되면 원래(정상) 상태로 복귀한다.
    ///
    /// 연동: 이 스크립트는 "연출" 전용이다. Sanity 침식·단계별 등장/잠복·정화는
    /// 같은 오브젝트에 붙는 AnomalyThreat / AnomalyPurifiable 이 담당하며,
    /// AnomalyThreat 가 등장/잠복에 맞춰 이 스크립트를 enable/disable 한다.
    /// </summary>
    public class ReversedClockAnomaly : MonoBehaviour
    {
        [Header("시계 바늘")]
        [SerializeField] private Transform hourHand;
        [SerializeField] private Transform minuteHand;
        [SerializeField] private Transform secondHand;
        [Tooltip("바늘이 도는 축(피벗의 로컬 기준, 보통 시계 정면 +Z).")]
        [SerializeField] private Vector3 rotationAxis = Vector3.forward;
        [Tooltip("바늘이 도는 중심점. 비우면 Clock Body(없으면 자기 자신)의 위치를 중심으로 공전한다.\n" +
                 "바늘이 엉뚱하게 날아가 사라지면, 시계 정중앙에 빈 GameObject를 만들어 여기에 연결하세요.")]
        [SerializeField] private Transform rotationPivot;

        [Header("거꾸로 거동")]
        [Tooltip("초침이 1초에 도는 각도(역방향으로 적용).")]
        [SerializeField] private float secondHandDegPerSec = 6f;
        [Tooltip("등장 시 시계 몸체를 180도 뒤집는다(거꾸로 걸림).")]
        [SerializeField] private bool hangUpsideDown = true;
        [Tooltip("뒤집을 대상. 비우면 이 오브젝트 자신.")]
        [SerializeField] private Transform clockBody;
        [SerializeField] private Vector3 upsideDownAxis = Vector3.forward;

        [Header("시스템 연동 (선택)")]
        [Tooltip("해리성 단계가 깊을수록(공격성↑) 바늘이 더 빨리 돈다.")]
        [SerializeField] private bool scaleSpeedByDissociation = true;
        [Tooltip("정신력이 낮을수록 바늘이 더 빨라진다.")]
        [SerializeField] private bool accelerateWhenLowSanity = true;

        private Quaternion bodyNormalRot;
        private bool cached;

        private void Awake()
        {
            if (clockBody == null) clockBody = transform;
            bodyNormalRot = clockBody.localRotation;
            cached = true;
        }

        private void OnEnable()
        {
            if (hangUpsideDown && clockBody != null)
                clockBody.localRotation = bodyNormalRot * Quaternion.AngleAxis(180f, upsideDownAxis.normalized);
        }

        private void OnDisable()
        {
            if (cached && clockBody != null)
                clockBody.localRotation = bodyNormalRot; // 정상 복귀
        }

        private void Update()
        {
            float mult = 1f;
            if (scaleSpeedByDissociation && DissociationLevelManager.Instance != null)
                mult *= DissociationLevelManager.Instance.CurrentEnemyAggression;
            if (accelerateWhenLowSanity && SanityManager.Instance != null)
                mult *= Mathf.Lerp(1.6f, 1f, SanityManager.Instance.SanityRatio); // 낮을수록 빠름

            float secDeg = -secondHandDegPerSec * mult * Time.deltaTime; // 음수 = 역방향

            // 바늘 pivot이 시계 중앙이 아니어도 정상 회전하도록, 중심점 기준 공전(RotateAround).
            Transform pivotT = rotationPivot != null ? rotationPivot : (clockBody != null ? clockBody : transform);
            Vector3 pivot = pivotT.position;
            Vector3 worldAxis = pivotT.TransformDirection(rotationAxis).normalized;
            if (secondHand != null) secondHand.RotateAround(pivot, worldAxis, secDeg);
            if (minuteHand != null) minuteHand.RotateAround(pivot, worldAxis, secDeg / 60f);
            if (hourHand != null) hourHand.RotateAround(pivot, worldAxis, secDeg / 720f);
        }
    }
}
