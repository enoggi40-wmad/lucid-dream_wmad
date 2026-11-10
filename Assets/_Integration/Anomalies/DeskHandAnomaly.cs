using UnityEngine;
using LucidDream.Core;

namespace LucidDream.Integration
{
    /// <summary>
    /// 이상현상: 책상 위의 손.
    /// 플레이어가 쳐다보지 않을 때 책상 위를 슬금슬금 기어오고, 시선이 향하면 멈춘다(위핑 엔젤식).
    /// 손가락은 까딱거린다. 잠복/정화로 비활성화되면 시작 위치/자세로 복귀한다.
    ///
    /// 연동: 연출 전용. Sanity 침식·등장/잠복·정화는 AnomalyThreat / AnomalyPurifiable 담당.
    /// </summary>
    public class DeskHandAnomaly : MonoBehaviour
    {
        [Header("손")]
        [Tooltip("이동시킬 손 Transform. 비우면 이 오브젝트 자신.")]
        [SerializeField] private Transform hand;
        [Tooltip("까딱일 손가락 관절들(선택).")]
        [SerializeField] private Transform[] fingers;

        [Header("기어오기")]
        [Tooltip("관찰되지 않을 때 기어가는 로컬 방향.")]
        [SerializeField] private Vector3 crawlDirectionLocal = Vector3.forward;
        [SerializeField] private float crawlSpeed = 0.25f;        // m/s
        [SerializeField] private float maxCrawlDistance = 1.2f;   // 시작점에서 최대 이동거리
        [Tooltip("플레이어가 쳐다보면 멈춘다.")]
        [SerializeField] private bool freezeWhenObserved = true;

        [Header("손가락 까딱")]
        [SerializeField] private float tapAmplitudeDeg = 18f;
        [SerializeField] private float tapSpeed = 6f;
        [SerializeField] private Vector3 tapAxis = Vector3.right;

        [Header("관찰 판정")]
        [Tooltip("카메라 정면 기준 이 각도(도) 안에 들어오면 '관찰됨'.")]
        [SerializeField] private float observeConeDeg = 35f;
        [SerializeField] private float observeMaxDistance = 14f;

        [Header("사운드 (선택)")]
        [SerializeField] private AudioSource tapAudio;

        private Vector3 handStartLocalPos;
        private Quaternion[] fingerStartRot;
        private float crawled;
        private Camera cam;

        private void Awake()
        {
            if (hand == null) hand = transform;
            handStartLocalPos = hand.localPosition;
            if (fingers != null)
            {
                fingerStartRot = new Quaternion[fingers.Length];
                for (int i = 0; i < fingers.Length; i++)
                    if (fingers[i] != null) fingerStartRot[i] = fingers[i].localRotation;
            }
        }

        private void OnEnable()
        {
            crawled = 0f;
            if (tapAudio != null) tapAudio.Play();
        }

        private void OnDisable()
        {
            if (hand != null) hand.localPosition = handStartLocalPos; // 정상 복귀
            if (fingers != null && fingerStartRot != null)
                for (int i = 0; i < fingers.Length; i++)
                    if (fingers[i] != null) fingers[i].localRotation = fingerStartRot[i];
            crawled = 0f;
            if (tapAudio != null && tapAudio.isPlaying) tapAudio.Stop();
        }

        private void Update()
        {
            // 손가락 까딱
            if (fingers != null && fingerStartRot != null)
            {
                float a = Mathf.Sin(Time.time * tapSpeed) * tapAmplitudeDeg;
                Vector3 axis = tapAxis.normalized;
                for (int i = 0; i < fingers.Length; i++)
                    if (fingers[i] != null)
                        fingers[i].localRotation = fingerStartRot[i] * Quaternion.AngleAxis(a, axis);
            }

            // 기어오기 (관찰되지 않을 때만)
            bool observed = freezeWhenObserved && IsObserved();
            if (!observed && crawled < maxCrawlDistance && hand != null)
            {
                float mult = DissociationLevelManager.Instance != null
                    ? DissociationLevelManager.Instance.CurrentEnemyAggression : 1f;
                float step = Mathf.Min(crawlSpeed * mult * Time.deltaTime, maxCrawlDistance - crawled);
                hand.localPosition += crawlDirectionLocal.normalized * step;
                crawled += step;
            }
        }

        private bool IsObserved()
        {
            if (cam == null) cam = Camera.main;
            if (cam == null || hand == null) return false;
            Vector3 to = hand.position - cam.transform.position;
            if (to.magnitude > observeMaxDistance) return false;
            return Vector3.Angle(cam.transform.forward, to) <= observeConeDeg;
        }
    }
}
