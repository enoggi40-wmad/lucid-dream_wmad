using UnityEngine;
using LucidDream.Core;
using System.Collections;

public class WJ_LowSanityEffectManager : MonoBehaviour
{
    public enum LowSanityEffect
    {
        BlurVision,
        SlowMovement,
        CameraShake,
        HearingLoss,
        ExtraSanityDamage
    }

    [Header("효과 설정")]
    [SerializeField] private LowSanityEffect[] possibleEffects;

    [Header("플레이어 컨트롤러")]
    [SerializeField] private PlayerController playerController;

    [Header("속도 감소 설정")]
    [SerializeField] private float slowDuration = 5f;
    [SerializeField] private float slowMultiplier = 0.5f;

    [Header("Sanity 추가 감소 속도")]
    [SerializeField] private float extraDecayPerSecond = 3f; // 초당 추가 감소량
    [SerializeField] private float extraDecayDuration = 5f;  // 추가 감소 지속 시간

    private Coroutine extraDecayCoroutine;

    private Coroutine slowCoroutine; // 속도 감소 Coroutine 저장용

    private void Start()
    {
        // SanityManager가 씬에 없으면 효과를 연결할 수 없음
        if (SanityManager.Instance == null)
        {
            Debug.LogWarning("[WJ_LowSanityEffectManager] SanityManager가 씬에 없습니다.");
            return;
        }

        // Sanity가 낮아졌을 때 ApplyRandomEffect 함수가 실행되도록 이벤트 등록
        SanityManager.Instance.OnLowSanityReached.AddListener(ApplyRandomEffect);
    }

    private void OnDestroy()
    {
        // 오브젝트가 삭제될 때 이벤트 연결 해제
        // 해제하지 않으면 나중에 삭제된 오브젝트를 호출하려고 해서 오류가 날 수 있음
        if (SanityManager.Instance == null) return;

        SanityManager.Instance.OnLowSanityReached.RemoveListener(ApplyRandomEffect);
    }

    /// <summary>
    /// 등록된 효과 목록 중 하나를 랜덤으로 선택해서 실행
    /// </summary>
    private void ApplyRandomEffect()
    {
        // possibleEffects 배열이 비어 있으면 랜덤 선택 불가
        if (possibleEffects == null || possibleEffects.Length == 0)
        {
            Debug.LogWarning("[WJ_LowSanityEffectManager] 등록된 효과가 없습니다.");
            return;
        }

        // 배열 범위 안에서 랜덤 인덱스 선택
        int randomIndex = Random.Range(0, possibleEffects.Length);
        LowSanityEffect selectedEffect = possibleEffects[randomIndex];

        Debug.Log("[WJ_LowSanityEffectManager] 선택된 효과: " + selectedEffect);

        // 선택된 효과에 맞는 함수 실행
        switch (selectedEffect)
        {
            case LowSanityEffect.BlurVision:
                ApplyBlurVision();
                break;

            case LowSanityEffect.SlowMovement:
                ApplySlowMovement();
                break;

            case LowSanityEffect.CameraShake:
                ApplyCameraShake();
                break;

            case LowSanityEffect.HearingLoss:
                ApplyHearingLoss();
                break;

            case LowSanityEffect.ExtraSanityDamage:
                ApplyExtraSanityDamage();
                break;
        }
    }

    /// <summary>
    /// 시야 흐림 효과
    /// 현재는 로그만 출력하고, 나중에 UI Image 또는 Post Processing 연결 예정
    /// </summary>
    private void ApplyBlurVision()
    {
        Debug.Log("[LowSanityEffect] 시야 흐림 효과 실행");

        // TODO: 화면 전체를 덮는 UI Image의 alpha를 올리거나
        // TODO: Post Processing Volume을 이용해서 Blur/Vignette 효과 연결
    }

    /// <summary>
    /// 이동 속도 감소 효과
    /// PlayerController의 SetMoveMultiplier 함수를 호출해서 이동속도 배율을 낮춤
    /// </summary>
    private void ApplySlowMovement()
    {
        Debug.Log("[LowSanityEffect] 이동 속도 감소 효과 실행");

        // PlayerController가 연결되지 않았다면 속도 감소를 적용할 수 없음
        if (playerController == null)
        {
            Debug.LogWarning("[WJ_LowSanityEffectManager] PlayerController가 연결되지 않았습니다.");
            return;
        }

        // 이미 속도 감소 Coroutine이 실행 중이라면 중복 실행 방지를 위해 정지
        if (slowCoroutine != null)
        {
            StopCoroutine(slowCoroutine);
        }

        // 속도 감소 Coroutine 시작
        slowCoroutine = StartCoroutine(SlowMovementRoutine());
    }

    /// <summary>
    /// 일정 시간 동안 이동속도를 낮췄다가 원래대로 복구하는 Coroutine
    /// </summary>
    private IEnumerator SlowMovementRoutine()
    {
        // PlayerController 내부의 moveSpeedMultiplier 값을 slowMultiplier로 변경
        // 예: slowMultiplier가 0.5면 최종 이동속도가 50%가 됨
        playerController.SetMoveMultiplier(slowMultiplier);

        // slowDuration초 동안 대기
        yield return new WaitForSeconds(slowDuration);

        // 지속 시간이 끝나면 이동속도 원상복구
        playerController.SetMoveMultiplier(1f);

        // Coroutine 종료 표시
        slowCoroutine = null;

        Debug.Log("[LowSanityEffect] 이동 속도 감소 효과 종료");
    }

    /// <summary>
    /// 화면 흔들림 효과
    /// </summary>
    private void ApplyCameraShake()
    {
        Debug.Log("[LowSanityEffect] 화면 흔들림 효과 실행");
        // 나중에 CameraShake 스크립트 연결
    }

    /// <summary>
    /// 청각 이상 효과
    /// </summary>
    private void ApplyHearingLoss()
    {
        Debug.Log("[LowSanityEffect] 청각 이상 효과 실행");

        // TODO: AudioMixer 볼륨 감소 또는 AudioLowPassFilter로 먹먹한 소리 구현
    }

    /// <summary>
    /// 추가 Sanity 감소 효과
    /// 즉시 데미지를 주는 것이 아니라, 일정 시간 동안 Sanity 자연 감소 속도를 증가시킴
    /// </summary>
    private void ApplyExtraSanityDamage()
    {
        Debug.Log("[LowSanityEffect] Sanity 감소 속도 증가 실행");

        if (SanityManager.Instance == null)
        {
            Debug.LogWarning("[WJ_LowSanityEffectManager] SanityManager가 없어서 추가 감소 속도 적용 불가");
            return;
        }

        // 이미 추가 감소 효과가 실행 중이면 중복 방지를 위해 기존 Coroutine 정지
        if (extraDecayCoroutine != null)
        {
            StopCoroutine(extraDecayCoroutine);
        }

        extraDecayCoroutine = StartCoroutine(ExtraDecayRoutine());
    }

    /// <summary>
    /// 일정 시간 동안 Sanity 감소 속도를 증가시키고, 시간이 끝나면 원래대로 되돌림
    /// </summary>
    private IEnumerator ExtraDecayRoutine()
    {
        // 초당 추가 감소량 적용
        // 예: baseDecayPerSecond가 0.8이고 extraDecayPerSecond가 3이면
        // 최종 감소량은 대략 초당 3.8 이상이 됨
        SanityManager.Instance.SetEnvironmentalFactor(extraDecayPerSecond);

        yield return new WaitForSeconds(extraDecayDuration);

        // 추가 감소량 제거
        SanityManager.Instance.SetEnvironmentalFactor(0f);

        extraDecayCoroutine = null;

        Debug.Log("[LowSanityEffect] Sanity 감소 속도 증가 종료");
    }
}