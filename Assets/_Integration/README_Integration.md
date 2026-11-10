# 시스템 통합 (Anomaly ↔ Purifying ↔ Sanity ↔ 해리성 단계)

이 폴더의 스크립트들은 기존에 따로 동작하던 세 시스템을 **유기적으로 연결**한다.
**기존 팀원 코드(JJY/Woojin/SanitySystem)는 한 줄도 수정하지 않았고**, 연결 전용
컴포넌트만 추가했다. 실제 연결은 Unity 인스펙터에서 컴포넌트를 붙이는 것으로 완성된다.

## 연결 흐름 (게임 루프)

```
        ┌──────────────────────────────────────────────────────────┐
        │                                                          ▼
  [해리성 단계 ↑] ──공격성 배수──▶ [Anomaly] ──Sanity 침식──▶ [Sanity ↓]
        ▲                                                          │
        │                                                          │ 30% 이하
        │ (정화로 단계 완화)                                        ▼
        │                                          [Sanity 저하 → 해리성 단계 강제 상승]
        │                                                          │
   [정화 효율 보정] ◀── 해리성 단계 ──┐                            (악순환)
        │                            │
        └──────[Purifying(정화 빔)]──┴──▶ Anomaly 무력화 + Sanity 회복 + 단계 완화
```

- **Anomaly → Sanity**: 이상현상이 활성 상태이고 플레이어가 가까이 있으면 정신력을 깎는다.
- **해리성 단계 → Anomaly**: 단계가 높을수록 침식 속도가 빨라진다(적 공격성 배수).
- **Sanity → 해리성 단계**: 정신력이 30% 이하로 떨어지면 단계가 강제 상승한다(기존 SanityManager 로직).
- **Purifying → Anomaly**: 정화 빔으로 이상현상을 정화하면 위협이 멈춘다.
- **Purifying → Sanity**: 정화 완료 시 정신력이 회복된다.
- **해리성 단계 → Purifying**: 단계가 높을수록 정화 회복 효율이 낮아진다(기존 효율 배수 활용).

## 스크립트

### 1. `AnomalyThreat.cs`
이상현상을 "위협"으로 만든다. 이상현상 GameObject에 붙인다.
- `baseSanityDrainPerSecond`: 초당 기본 정신력 침식량(해리성 공격성 배수가 곱해짐).
- `threatRange`: 침식 발동 반경(0이면 거리 무시, 항상 침식).
- `requireLineOfSight`: 벽에 가려지면 침식 안 함.
- `difficulty`: **이 이상현상의 난이도 등급(Easy 하 / Medium 중 / Hard 상).**
  현재 해리성 단계의 "활성 등급"과 일치할 때만 등장한다.
  => 단계 ↑(해리 심화)면 Easy(하)가 등장 + 정신력 소모 빨라짐, 단계 ↓(자각)면 Hard(상)가 등장 + 소모 느려짐.
  단계가 오르내리면 재등장/잠복이 반복되고, 정화(Neutralize)만 영구적이다.
- `disableSiblingsWhenDormant`: 잠복(미등장) 시 동작 스크립트를 꺼둘지 여부.
- `Neutralize()`: 정화 시 호출되어 위협을 영구히 멈추고 이상현상 동작 스크립트를 끈다.

### 2. `AnomalyPurifiable.cs`  ← 핵심
JJY의 `PurifiableObject`를 상속한 "정화 가능한 이상현상". 정화가 끝나면
이상현상 무력화 + 정신력 회복 + (옵션)해리성 단계 완화를 한 번에 수행한다.
- `sanityRewardOnComplete`: 정화 완료 시 회복량(정화 효율로 보정).
- `reduceDissociationOnComplete`: 정화로 해리성 단계를 낮출지 여부.

### 3. `PurifyReward.cs`
이상현상이 아닌 **일반 정화 대상**(예: `HospitalChartPurifiable`)에도
"정화 → 정신력 회복"을 붙여주는 어댑터. 기존 코드 수정 없이 컴포넌트만 추가하면 된다.

## 적용 방법 (인스펙터 작업)

씬에는 `SanityManager`, `DissociationLevelManager`가 각각 하나씩 존재해야 한다
(없으면 연결 스크립트는 안전하게 무시되며 에러는 나지 않는다).

**이상현상 오브젝트(예: 초상화/식물/해부모형 등)에:**
1. `Collider` 추가 (정화 빔 레이캐스트 대상이 되도록, `PurifyController`의 `targetLayerMask`에 포함).
2. `AnomalyThreat` 추가 → 침식량/반경 설정.
3. `AnomalyPurifiable` 추가 → `requiredPurifyTime`, `sanityRewardOnComplete` 설정.
   - 정화 완료 시 같은 오브젝트의 `AnomalyThreat`를 자동으로 찾아 무력화한다.
4. 기존 Woojin 이상현상 스크립트(WJ_Anomaly_*)는 그대로 두면 된다.
   정화되면 `AnomalyThreat`가 이들을 자동으로 비활성화한다.

**일반 정화 대상(차트 등)에:**
1. `PurifyReward` 추가 → 회복량 설정. (같은 오브젝트의 `PurifiableObject`를 자동 인식.)

## 에디터 자동 배선 도구 (`Editor/AnomalyIntegrationTools.cs`)

씬 파일을 직접 편집하지 않고, 상단 메뉴에서 컴포넌트를 안전하게 붙일 수 있다.
(Editor 폴더에 있어 빌드에는 포함되지 않는다. Undo 지원.)

- **`Tools > Lucid Dream > Wire Anomalies In Open Scene`**
  현재 열린 씬에서 `WJ_Anomaly*` 스크립트가 붙은 오브젝트를 모두 찾아 `AnomalyThreat`를
  자동으로 추가한다. Collider가 없는 오브젝트는 경고로 알려준다.
- **`Tools > Lucid Dream > Make Selected Purifiable Anomaly`**
  Hierarchy에서 선택한 오브젝트를 "정화 가능한 이상현상"으로 만든다
  (`AnomalyThreat` + `AnomalyPurifiable` + 필요 시 `BoxCollider` 추가).

권장 흐름: ① 씬 열기 → ② "Wire Anomalies In Open Scene" 실행 → ③ 정화로 없앨 이상현상만
선택 후 "Make Selected Purifiable Anomaly" 실행 → ④ 각 컴포넌트의 수치(침식량/회복량/
`difficulty` 등급)을 인스펙터에서 조정.

## 설계 메모
- 단일 어셈블리(Assembly-CSharp)라 `LucidDream.Integration` ↔ `LucidDream.Core` 참조가 자유롭다.
- 모든 연결은 싱글톤(`SanityManager.Instance`, `DissociationLevelManager.Instance`)이
  없을 때 null 가드로 안전하게 통과하므로, 일부 시스템만 있는 씬에서도 컴파일/실행된다.
