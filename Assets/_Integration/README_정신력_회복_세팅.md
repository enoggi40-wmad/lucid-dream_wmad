# 정신력(Sanity) 회복 / 일시정지 기능 — 세팅 가이드

프로토타입 난이도 조절을 위한 정신력 회복·정지 기능 3종입니다.
기존 시스템(`SanityManager` 싱글톤, `DontDestroyOnLoad`)을 그대로 쓰며, 연결용 컴포넌트는
모두 `Assets/_Integration/`에 있습니다.

| 기능 | 스크립트 | 위치 |
|---|---|---|
| ① Passive Decay 일시정지 API | `SanityManager` (메서드 추가) | `Assets/SanitySystem/` |
| ② 회복존 (난이도 조절용) | `SanityRecoveryZone` | `Assets/_Integration/` |
| ③ 스테이지 전환 시 정신력 회복 | `StageTransitionSanity` | `Assets/_Integration/` |
| ④ 꿈 판별 UI 동안 정신력 정지 (드롭인) | `SanityFreezeBinding` + `SanityManager` freeze API | `Assets/_Integration/` |

---

## ① Passive Decay 일시정지

"상황 파악 시간"(꿈 판별 UI가 뜨기 전 등)에 자연 감소(Passive Decay)를 멈추는 용도입니다.

`SanityManager`에 추가된 API:

```csharp
SanityManager.Instance.PausePassiveDecay();   // 자연 감소 멈춤
SanityManager.Instance.ResumePassiveDecay();  // 멈춤 해제 (Pause와 1:1로 짝 맞추기)
SanityManager.Instance.ResetPassiveDecayPause(); // 모든 멈춤 강제 해제
bool paused = SanityManager.Instance.IsPassiveDecayPaused;
```

- **참조 카운트 방식**입니다. 여러 곳(예: UI 창 + 회복존)이 동시에 멈춰도 안전하며,
  `Pause`를 부른 만큼 `Resume`을 불러야 다시 감소합니다.
- 멈추는 것은 **자연 감소(Passive)만** 입니다. 이상현상 피해(`ApplyDamage`)와
  안전지대 회복은 영향을 받지 않습니다.

### UI 연동 예시

```csharp
// 꿈 판별 UI를 띄우기 직전
SanityManager.Instance.PausePassiveDecay();
ShowDreamJudgementUI();

// 플레이어가 상황 파악을 끝내고 UI를 닫을 때
HideDreamJudgementUI();
SanityManager.Instance.ResumePassiveDecay();
```

> 주의: 화면을 띄울 때 `Pause`를 부르면, 어떤 경로로 닫히든 **반드시** `Resume`을
> 부르도록 하세요(짝이 안 맞으면 감소가 영영 멈춤). UI를 끄는 `OnDisable`에 `Resume`을
> 넣어 두면 안전합니다.

### 정신력 완전 정지 (freeze) API

`PausePassiveDecay`는 **자연 감소만** 멈춥니다. 꿈 판별 UI처럼 "그동안 정신력이 **전혀**
떨어지면 안 되는" 경우(이상현상 피해까지 차단)에는 freeze API를 씁니다.

```csharp
SanityManager.Instance.FreezeSanity();    // 모든 감소 차단 (Passive + ApplyDamage)
SanityManager.Instance.UnfreezeSanity();  // 해제 (Freeze와 1:1로 짝 맞추기)
SanityManager.Instance.ResetSanityFreeze();
bool frozen = SanityManager.Instance.IsSanityFrozen;
```

- 역시 **참조 카운트** 방식. `Heal()`(회복/증가)은 freeze 중에도 허용됩니다(감소만 차단).
- 보통은 아래 ④의 `SanityFreezeBinding` 컴포넌트로 자동 처리하므로 직접 호출할 일은 적습니다.

---

## ② 회복존 (`SanityRecoveryZone`)

맵 구간별로 정신력을 회복시키는 트리거 존입니다. 구간마다 회복 속도를 다르게 줄 수 있어
난이도 튜닝에 적합합니다.

### 배치 방법

1. 메뉴 **Tools ▸ Lucid Dream ▸ Sanity ▸ Create Recovery Zone** 클릭
   → `BoxCollider(IsTrigger)` + `SanityRecoveryZone`가 붙은 오브젝트가 씬 뷰 중앙에 생성됩니다.
2. 위치·크기(BoxCollider Size)를 회복 구간에 맞게 조정합니다.
3. 플레이어 오브젝트의 **Tag가 `Player`** 인지 확인합니다. (기본 감지 태그)

> 수동 배치: 빈 오브젝트에 트리거 Collider(Box/Sphere)를 추가하고 `SanityRecoveryZone`를
> 붙여도 됩니다. (`Reset`에서 IsTrigger 자동 설정)

### 인스펙터 필드

| 필드 | 설명 | 기본값 |
|---|---|---|
| Player Tag | 회복 대상으로 인식할 태그 | `Player` |
| Regen Per Second | 존 안에서 초당 회복하는 정신력 | `6` |
| Pause Passive Decay Inside | 존 안에서 자연 감소를 멈출지 (회복이 감소와 상쇄되지 않게) | `true` |
| Reduce Dissociation On Enter | 처음 진입 시 해리성 단계 1회 완화 | `false` |
| Dissociation Reduction Amount | 위가 켜졌을 때 낮출 단계 수 | `1` |

- `SetSafeZone`(전역 bool)을 쓰지 않고 `Heal()`로 직접 회복하므로 **존이 겹쳐도** 안전합니다.
- 존이 비활성화/파괴되거나 씬이 언로드돼도 멈춰 둔 감소를 `OnDisable`에서 자동 복구합니다.

### 기존 `SafeZone`과의 차이

| | `SafeZone` (SanitySystem) | `SanityRecoveryZone` (_Integration) |
|---|---|---|
| 회복률 | 전역 `safeZoneRegenPerSecond` 공유 | **존별 개별 설정** |
| 자연 감소 | 영향 없음(별도) | 옵션으로 존 안에서 정지 |
| 용도 | 일반 안전지대 | **구간별 난이도 튜닝** |

둘은 공존 가능합니다. 난이도 조절이 목적이면 `SanityRecoveryZone`을 권장합니다.

---

## ③ 스테이지 전환 시 회복 (`StageTransitionSanity`)

다음 스테이지(씬)로 넘어갈 때마다 정신력을 일정량 회복시킵니다.
`SanityManager`가 `DontDestroyOnLoad`라 씬을 넘어가도 정신력이 유지되는 점을 활용합니다.

### 세팅 방법

1. 메뉴 **Tools ▸ Lucid Dream ▸ Sanity ▸ Create Stage Recovery Manager** 클릭
   → `StageTransitionSanity` 오브젝트가 생성됩니다. (싱글톤 + DontDestroyOnLoad)
2. **첫 스테이지/타이틀 씬**에 한 개만 두면 됩니다. 자기 자신이 생성된 "현재 씬"은
   회복 대상이 아니고, 그 **이후의 전환부터** 적용됩니다.

### 인스펙터 필드

| 필드 | 설명 | 기본값 |
|---|---|---|
| As Percent Of Max | Amount를 최대 정신력의 %로 해석할지 | `false` |
| Amount | 전환마다 회복할 양 (퍼센트 모드면 0~100) | `30` |
| Stage Scene Names | 비어 있지 않으면 이 이름의 씬으로 갈 때만 회복 | (비움) |
| Excluded Scenes | 회복을 막을 씬 이름 (예: 타이틀/메뉴) | `Title` |
| Also Reduce Dissociation | 전환 시 해리성 단계도 1 낮춤 | `false` |

- 보통은 `Excluded Scenes`에 타이틀/메뉴 씬만 넣고 나머지는 비워 두면, 게임플레이 씬으로
  전환할 때마다 회복됩니다.
- 특정 스테이지에서만 회복시키려면 `Stage Scene Names`에 해당 씬 이름들을 적습니다.
- 코드/버튼에서 즉시 회복시키려면 `StageTransitionSanity.Instance.RecoverNow()` 또는
  `OnStageRecovery` UnityEvent를 사용하세요.

---

## ④ 꿈 판별 UI 동안 정신력 정지 (`SanityFreezeBinding`)

"꿈 판별 UI(`dreamQuestionPanel`)가 떠 있는 동안 Sanity가 감소하지 않게" 하는 드롭인 컴포넌트.
판별 패널은 `SetActive(true/false)`로 켜고 꺼지므로, 패널 오브젝트에 이 컴포넌트만 붙이면
`OnEnable`에서 잠그고 `OnDisable`에서 풀어 줍니다. **코드 수정·기존 스크립트 변경 없음.**

### 세팅 방법 (둘 중 하나)

**A. 컴포넌트만 붙이기 (권장 — 코드 0줄)**
1. 판별 UI 패널 오브젝트(`WJ_AnomalyManager`의 `dreamQuestionPanel`로 연결된 그 패널)를 선택.
2. **Add Component ▸ `SanityFreezeBinding`** 추가.
3. `Mode`를 `FreezeAll`(기본, 모든 감소 차단)로 둡니다. 자연 감소만 멈추고 이상현상 피해는
   유지하고 싶으면 `PassiveOnly`로.
4. 끝. 패널이 떠 있는 동안 정신력이 고정됩니다.

**B. 코드에서 직접 (패널을 SetActive로 토글하지 않는 구조일 때)**
```csharp
// 판별 UI를 띄울 때
SanityManager.Instance.FreezeSanity();
// 판별이 끝나 UI를 닫을 때
SanityManager.Instance.UnfreezeSanity();
```

> `SanityFreezeBinding`은 `Apply()`/`Release()` public 메서드도 제공하므로,
> 버튼 `OnClick`이나 다른 이벤트에 직접 연결할 수도 있습니다.

### 참고: 판별 전 "상황 파악 시간"

판별 UI가 뜨기 *전* 대기 시간에만 자연 감소를 멈추고 싶다면 ①의 `PausePassiveDecay`를
그 구간 시작/끝에 호출하면 됩니다. (이상현상 피해는 그대로 두는 경우)

---

## 커밋 / 브랜치 가이드

- 추가 파일: `Assets/_Integration/SanityRecoveryZone.cs`,
  `Assets/_Integration/StageTransitionSanity.cs`,
  `Assets/_Integration/SanityFreezeBinding.cs`,
  `Assets/_Integration/Editor/SanityToolsExtra.cs`, 본 문서.
- 수정 파일: `Assets/SanitySystem/SanityManager.cs` (pause/freeze 메서드 + Update·ApplyDamage 가드 추가, 기존 로직 불변).
- 신규 코드는 모두 `_Integration`에 두어 다른 작업과 충돌을 최소화했습니다.
- Unity가 새 `.cs`마다 `.meta`를 생성하므로, **`.meta` 파일도 함께 커밋**하세요.
- 권장: 본인 작업 브랜치에서 커밋 → PR로 main 병합.
