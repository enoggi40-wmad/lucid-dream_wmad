using System.Collections.Generic;
using UnityEngine;
using LucidDream.Core;

namespace LucidDream.Integration
{
    /// <summary>
    /// 이상현상: 거울 속 '나'가 다른 행동을 한다.
    /// 거울에 비친 반사상(플레이어와 똑같은 모델)이 평소에는 플레이어를 정상적으로 따라(반사)하지만,
    /// 일정 간격마다 잠깐 '불일치(mismatch)' 상태가 되어 플레이어와 다른 행동을 한다.
    ///   · Freeze      : 플레이어가 움직여도 반사상은 가만히 멈춰 있는다.
    ///   · KeepStaring : 플레이어가 등을 돌려도 반사상은 계속 이쪽을 쳐다본다.
    ///   · StepCloser  : 반사상이 거울 쪽으로 슬금슬금 다가온다.
    ///   · LagBehind   : 반사상이 한 박자 늦게 따라온다(기존 '지연' 연출).
    /// 정신력이 낮을수록 불일치가 더 자주 일어난다.
    ///
    /// 사용법: 거울 표면 오브젝트에 이 컴포넌트를 붙이고
    ///  - mirrorPlane(비우면 자기 자신) 의 위치/법선을 거울 평면으로 사용
    ///  - reflectionProxy 에 '플레이어와 똑같은 모델'(반사상) Transform 을 연결
    /// 하면, 반사상이 거울 너머에서 평소엔 정상 반사하다가 가끔 다른 행동을 한다.
    ///
    /// 연동: 연출 전용. Sanity 침식·등장/잠복·정화는 AnomalyThreat / AnomalyPurifiable 담당.
    /// </summary>
    public class MirrorDelayAnomaly : MonoBehaviour
    {
        public enum MismatchBehavior { Freeze, KeepStaring, StepCloser, LagBehind }

        [Header("거울 평면")]
        [Tooltip("거울 표면 Transform. 위치/법선(아래 축)을 거울 평면으로 쓴다. 비우면 자기 자신.")]
        [SerializeField] private Transform mirrorPlane;
        [Tooltip("거울 평면의 법선(로컬). 보통 forward(+Z).")]
        [SerializeField] private Vector3 planeNormalLocal = Vector3.forward;

        [Header("반사상 (플레이어와 똑같은 모델)")]
        [Tooltip("거울에 비치는 반사상. 플레이어와 동일한 모델을 연결한다. 평소엔 플레이어를 정상 반사하고, 불일치 때만 다른 행동을 한다.")]
        [SerializeField] private Transform reflectionProxy;
        [Tooltip("KeepStaring 시 머리만 돌릴 머리 본(선택). 비우면 몸 전체가 플레이어를 향한다.")]
        [SerializeField] private Transform headBone;

        [Header("추적 대상")]
        [Tooltip("반사할 대상(보통 플레이어). 비우면 SanityManager/Player 태그로 탐색.")]
        [SerializeField] private Transform target;
        [Tooltip("플레이어가 거울에서 이 거리 안일 때만 동작한다.")]
        [SerializeField] private float activateDistance = 12f;

        [Header("불일치(다른 행동) 연출")]
        [Tooltip("불일치 사이의 평소 간격(초). 이 시간 동안은 정상 반사한다.")]
        [SerializeField] private float mismatchInterval = 8f;
        [Tooltip("한 번의 불일치가 지속되는 시간(초).")]
        [SerializeField] private float mismatchDuration = 2.5f;
        [Tooltip("정신력이 낮을수록 불일치 간격을 줄인다(더 자주 발생).")]
        [SerializeField] private bool moreFrequentWhenLowSanity = true;
        [Tooltip("정신력이 0일 때의 최소 간격(초).")]
        [SerializeField] private float minInterval = 2.5f;
        [Tooltip("불일치마다 행동을 무작위로 고른다. 끄면 아래 고정 행동만 사용한다.")]
        [SerializeField] private bool randomizeBehavior = true;
        [Tooltip("randomizeBehavior가 꺼졌을 때 사용할 고정 행동.")]
        [SerializeField] private MismatchBehavior behavior = MismatchBehavior.KeepStaring;

        [Header("행동 세부")]
        [Tooltip("StepCloser: 거울 쪽으로 다가오는 속도(m/s).")]
        [SerializeField] private float stepCloserSpeed = 0.4f;
        [Tooltip("LagBehind: 늦게 따라오는 지연(초).")]
        [SerializeField] private float lagSeconds = 0.7f;

        private struct Sample { public Vector3 pos; public Quaternion rot; public float t; }
        private readonly Queue<Sample> buffer = new Queue<Sample>();

        private bool inMismatch;
        private float stateTimer;
        private MismatchBehavior current;

        private void OnEnable()
        {
            buffer.Clear();
            inMismatch = false;
            stateTimer = 0f;
            if (reflectionProxy != null) reflectionProxy.gameObject.SetActive(true);
        }

        private void OnDisable()
        {
            buffer.Clear();
            if (reflectionProxy != null) reflectionProxy.gameObject.SetActive(false); // 정상(반사상 없음) 복귀
        }

        private void LateUpdate()
        {
            if (reflectionProxy == null) return;
            if (target == null) ResolveTarget();
            if (target == null) return;

            Transform plane = mirrorPlane != null ? mirrorPlane : transform;
            if (Vector3.Distance(target.position, plane.position) > activateDistance) return;

            // 1) 거울 평면에 플레이어를 반사한 '정상 반사' 위치/회전
            Vector3 n = plane.TransformDirection(planeNormalLocal).normalized;
            Vector3 reflPos = Reflect(target.position, plane.position, n);
            Quaternion reflRot = ReflectRotation(target.rotation, n);

            // 2) LagBehind 용으로 정상 반사 샘플을 항상 기록(약 4초 분량)
            float now = Time.time;
            buffer.Enqueue(new Sample { pos = reflPos, rot = reflRot, t = now });
            while (buffer.Count > 240) buffer.Dequeue();

            // 3) 정상 ↔ 불일치 상태 전환
            stateTimer += Time.deltaTime;
            if (!inMismatch)
            {
                if (stateTimer >= CurrentInterval())
                {
                    inMismatch = true;
                    stateTimer = 0f;
                    current = randomizeBehavior
                        ? (MismatchBehavior)Random.Range(0, System.Enum.GetValues(typeof(MismatchBehavior)).Length)
                        : behavior;
                }
            }
            else if (stateTimer >= mismatchDuration)
            {
                inMismatch = false;
                stateTimer = 0f;
            }

            // 4) 평소: 정상 반사
            if (!inMismatch)
            {
                reflectionProxy.SetPositionAndRotation(reflPos, reflRot);
                return;
            }

            // 5) 불일치: 플레이어와 다른 행동
            switch (current)
            {
                case MismatchBehavior.Freeze:
                    // 위치/회전을 갱신하지 않는다 → 플레이어가 움직여도 반사상은 가만히 있다.
                    break;

                case MismatchBehavior.KeepStaring:
                {
                    // 거울 속 자리(정상 반사 위치)는 유지하되, 플레이어를 계속 쳐다본다.
                    Vector3 toPlayer = target.position - reflectionProxy.position;
                    toPlayer.y = 0f;
                    if (headBone != null)
                    {
                        reflectionProxy.SetPositionAndRotation(reflPos, reflRot);
                        if (toPlayer.sqrMagnitude > 1e-4f)
                            headBone.rotation = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
                    }
                    else
                    {
                        reflectionProxy.position = reflPos;
                        if (toPlayer.sqrMagnitude > 1e-4f)
                            reflectionProxy.rotation = Quaternion.LookRotation(toPlayer.normalized, Vector3.up);
                    }
                    break;
                }

                case MismatchBehavior.StepCloser:
                {
                    // 거울 평면 쪽으로 슬금슬금 다가오면서 플레이어를 본다.
                    Vector3 towardMirror = plane.position - reflectionProxy.position;
                    towardMirror.y = 0f;
                    if (towardMirror.sqrMagnitude > 1e-4f)
                        reflectionProxy.position += towardMirror.normalized * stepCloserSpeed * Time.deltaTime;

                    Vector3 toP = target.position - reflectionProxy.position;
                    toP.y = 0f;
                    if (toP.sqrMagnitude > 1e-4f)
                        reflectionProxy.rotation = Quaternion.LookRotation(toP.normalized, Vector3.up);
                    break;
                }

                case MismatchBehavior.LagBehind:
                {
                    // 정상 반사를 lagSeconds 만큼 늦게 따라온다.
                    Sample chosen = buffer.Peek();
                    foreach (var s in buffer)
                    {
                        if (now - s.t >= lagSeconds) chosen = s;
                        else break;
                    }
                    reflectionProxy.SetPositionAndRotation(chosen.pos, chosen.rot);
                    break;
                }
            }
        }

        /// <summary>현재 불일치 발생 간격(정신력이 낮을수록 짧아진다).</summary>
        private float CurrentInterval()
        {
            if (moreFrequentWhenLowSanity && SanityManager.Instance != null)
                return Mathf.Lerp(minInterval, mismatchInterval, SanityManager.Instance.SanityRatio);
            return mismatchInterval;
        }

        private static Vector3 Reflect(Vector3 point, Vector3 planePoint, Vector3 n)
        {
            float d = Vector3.Dot(point - planePoint, n);
            return point - 2f * d * n;
        }

        private static Quaternion ReflectRotation(Quaternion rot, Vector3 n)
        {
            Vector3 f = Vector3.Reflect(rot * Vector3.forward, n);
            Vector3 u = Vector3.Reflect(rot * Vector3.up, n);
            if (f.sqrMagnitude < 1e-6f) return rot;
            return Quaternion.LookRotation(f, u);
        }

        private void ResolveTarget()
        {
            if (SanityManager.Instance != null) target = SanityManager.Instance.transform;
            if (target == null)
            {
                var go = GameObject.FindGameObjectWithTag("Player");
                if (go != null) target = go.transform;
            }
        }
    }
}
