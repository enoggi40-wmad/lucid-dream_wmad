# Lucid Dream 프로젝트 — Claude 작업 메모리

## 저장소
- **경로**: `C:\Users\Administrator\Documents\GitHub\lucid-dream`
- **원격**: https://github.com/xenon201313/lucid-dream
- **엔진**: Unity 6 (6000.4.3f1)
- **브랜치**: main (팀 통합 완료)

---

## 게임 개요

- **제목**: LUCID DREAM (자각몽)
- **장르**: 1인칭 심리 호러 / 꿈 정화(Purification) 어드벤처 / 싱글 플레이 / PC(Windows)
- **컨셉**: ~~정신병원~~ → **꿈을 정화하는 컨셉**으로 변경 완료. 현재 배경 = **학교** (복도·교실·미술실·음악실, 그레이박스 단계)
- **태그라인 후보**: "이상한 게 있습니다. 찾아서 정화하세요. 당신의 정신력은, 지금 이 순간에도 깎이고 있습니다."

---

## 팀 구성 & 브랜치

| 브랜치 | 담당 |
|---|---|
| woojin | Woojin — 이상현상 스크립트, 악몽 시스템 |
| Purifying | JJY — 정화 빔 시스템, 플레이어 컨트롤러 |
| kimdajin | kimdajin |
| kimgyeonu | kimgyeonu — 학교 맵 제작 |
| yujin | yujin |

- **통합 완료**: 5개 브랜치를 `merge.directoryRenames=false`로 충돌 없이 main에 병합.

---

## 지금까지 한 작업

### 1. 브랜치 통합 (완료)
- 5개 브랜치 → main 병합 (누락 파일 0건 검증)
- horror-school 맵(34개) → `Assets/JJY/Resource/Map/` 경로 보존
- 의도된 삭제 3건만 존재 (PurifyBeamController.cs 리팩터, kimdajin 테스트 스크립트)

### 2. 시스템 통합 스크립트 (`Assets/_Integration/`)
기존 팀원 코드 **한 줄도 수정하지 않고** 연결 전용 컴포넌트 추가.

| 파일 | 역할 |
|---|---|
| `AnomalyThreat.cs` | 이상현상 → Sanity 침식. 해리성 단계 공격성 배수 적용. `minActivationLevel`로 단계별 각성 |
| `AnomalyPurifiable.cs` | PurifiableObject 상속. 정화 완료 시 이상현상 무력화 + Sanity 회복(효율 보정) + 단계 완화 |
| `PurifyReward.cs` | 일반 정화 대상에도 정화→Sanity 회복 보상 부여 (어댑터) |
| `Editor/AnomalyIntegrationTools.cs` | Unity 메뉴 Tools ▸ Lucid Dream으로 씬에 컴포넌트 자동 배선 |

**게임 루프**: 이상현상이 정신력 침식 → 정신력 30%↓ 시 해리성 단계 강제 상승(악순환) ↔ 정화로 무력화+회복+단계완화(선순환)

### 3. 문서/산출물
- `바탕화면\LucidDream_변경사항_정리_20260603.pdf` — 브랜치 통합·시스템 연결 정리 (팀 공유용)
- `바탕화면\LucidDream_포스터_내용_원고.pdf` — 포스터 텍스트 원고 (그래픽팀 전달용, 3페이지)
- `바탕화면\_poster_assets\ppt_images\` — 현재 컨셉 이미지 5종 (타이틀씬·학교 맵 스크린샷)
- `바탕화면\_poster_assets\poster_screens\` — **(6/12 추가)** EASY 단계 이상현상 포스터용 스크린샷 4종 (배경 어둡게 편집 완료). 5월 4주차 발표 PPT에 임베드된 mp4 5개에서 추출 (칠판 낙서 3컷 + 식물 Level1 1컷). 원본 영상: `_poster_assets\ppt_videos\`

### 4. 프로토타입 스크립트 (6/12 추가, 7/2 프로토타입 목표)
- `_Integration/AnomalyDirector.cs` — 씬의 모든 AnomalyPurifiable 추적, 전부 정화 시 OnAllPurified
- `_Integration/GameFlowManager.cs` — 게임 상태(Playing/Paused/Cleared/GameOver), ESC 일시정지, 씬 전환
- `_Integration/UI/` — SettingsManager(PlayerPrefs), TitleScreenUI, SettingsPanelUI, PauseMenuUI, GameHUD, GameResultUI
- `_Integration/Editor/UISetupTools.cs` — **Tools ▸ Lucid Dream ▸ UI ▸ Build Title Scene UI / Build In-Game UI** 메뉴로 UI 전체 자동 생성·배선 (한국어 폰트 Hakgyoansim 자동 적용, 비네트 스프라이트 자동 생성, 매니저 자동 생성)
- `_Integration/README_프로토타입_세팅.md` — 팀 공유용 세팅 가이드 (메뉴 클릭 순서)
- 주의: 기존 .cs 일부(SanitySystem 등)는 CP949 인코딩. 새 파일은 전부 UTF-8 BOM.

---

## 핵심 시스템

### 아키텍처: 싱글톤(`static Instance`) + 씬 전환 상태 유지
- 매니저는 `static Instance` 싱글톤 패턴 사용(`X.Instance`). Awake에서 중복 가드(`Destroy`) 후 `Instance = this`.
- **씬(Level1→2→3) 전환 시 상태 유지**: `SanityManager`·`DissociationLevelManager`는 `DontDestroyOnLoad`로 영속 → 해리성 단계·정신력이 씬을 넘어 유지된다(이우진 설계: 단계에 따라 스테이지 전환, 클리어로 단계 낮아지면 다음 Level로). `SettingsManager`도 설정 영속.
- 나머지(`AnomalyDirector`·`GameFlowManager`·`WJ_AnomalyManager`)는 씬별 단위라 영속하지 않는다.

### SanityManager (LucidDream.Core)
- Max Sanity 100, Passive Decay (해리성 배수 적용), 30% 이하 = LowSanity 트리거
- `ApplyDamage(amount, DamageType)` / `Heal(amount)` / `SetEnvironmentalFactor()`
- 싱글톤: `SanityManager.Instance` (DontDestroyOnLoad — 씬 전환 시 정신력 유지)

### DissociationLevelManager (LucidDream.Core)
- 5단계: Lv1 자각(×1.0) → Lv2 혼돈(×1.2) → Lv3 침식(×1.5) → Lv4 망각(×2.0) → Lv5 해리(×3.0)
- `CurrentEnemyAggression`, `CurrentPurificationEfficiency`, `CurrentActiveDifficulty`
- `LevelData.level` = **해리성 단계(1~5) 그 자체** (이우진 지적 맞음)
- **단계별 활성 난이도 등급(`activeDifficulty`)**: 단계가 등장하는 이상현상의 난이도 등급을 정한다.
  - **단계 ↑ (해리 심화) → 난이도 하(Easy) 등장 + 정신력 소모 빨라짐**
  - **단계 ↓ (자각 상태) → 난이도 상(Hard) 등장 + 정신력 소모 느려짐**
  - 매핑: Lv1·2 → Hard(상), Lv3 → Medium(중), Lv4·5 → Easy(하). 정신력 소모는 `decayMultiplier`(Lv1 ×1.0 → Lv5 ×3.0)가 담당.
  - 이상현상은 `AnomalyThreat.difficulty`가 현재 활성 등급과 일치할 때만 등장(`AnomalyDifficulty` enum, `LucidDream.Core`). 단계가 오르내리면 재등장/잠복 가능, 정화(Neutralize)는 영구.
- `TemporaryReduce(int)` — 정화 시 단계 완화
- 싱글톤: `DissociationLevelManager.Instance` (DontDestroyOnLoad — 씬 전환 시 해리성 단계 유지)

### PurifiableObject (JJY, 전역 네임스페이스)
- `AddPurifyTime()`, `CompletePurification()`, `OnPurifyComplete()` 오버라이드 가능
- `AnomalyPurifiable`이 이를 상속

### 이상현상 (구현됨)
- **Woojin (`WJ_Anomaly_*`, 연출 MonoBehaviour 5종)**: `PlantGrowAnomaly`(식물·시각) / `AnatomyModelAnomaly`(해부모형·시각) / `PortraitAnomaly`(초상화·시각) / `DoorKnock`(문 노크·청각) / `FloatingObject`(부유 물체·시각)
- **JJY (`Script/Object`)**: `BlackboardGraffitiAnomaly`(칠판 낙서·시각=하) / `LockerPhoneAnomaly`(사물함 휴대폰·청각=중) / `WanderingEntity`(배회체·경비원 NPC, NavMesh 순찰+플레이어 포획=상)
  - 배회체 부속: `FootprintPurifiable`(배회체 발자국 = 정화 포인트) · `RuntimeRoadNavMeshBuilder`(배회 경로 NavMesh 런타임 생성). `HospitalChartPurifiable`은 일반 정화 대상(이상현상 아님).
- 각 이상현상의 등장 난이도 등급은 `AnomalyThreat.difficulty`로 지정(단계↑→하 등장+소모↑, 단계↓→상 등장+소모↓).
- `WJ_NightmareChase` — Sanity 0 시 추격 시작 (SanityManager.OnSanityBreak 구독)
- `WJ_LowSanityEffectManager` — LowSanity 시 랜덤 효과

---

## 학교 이상현상 기획 (5월 4주차)

| 난이도 | 이상현상 | 정화 포인트 |
|---|---|---|
| 하 | 칠판 낙서 (시각) | 칠판 전체 영역 |
| 중 | 사물함 속 휴대폰 (청각) | 해당 사물함 |
| 상 | 배회체 Wanderer (경비원 NPC) | 남긴 손바닥·발자국 자국 |

추가 후보(채택 예정): 끝나지 않는 복도 / 거꾸로 걸린 시계 / 책상 위의 손 / 거울 속 지연

---

## 정화 UI 변경 (적용 완료)
- **Before**: FPS형 조준선 + 프로그레스 바
- **After**: 정화 대상 **외곽선 하이라이트** + 상호작용 → 정화 방식 (화면 복잡도↓)
- 정화 이펙트 사운드 추가 완료

---

## 남은 작업
- [ ] **(7/2 프로토타입)** Unity 에디터에서 `README_프로토타입_세팅.md` 순서대로 씬 세팅:
      School 씬에서 Wire Anomalies → Make Selected Purifiable → Build In-Game UI,
      Title 씬 생성 후 Build Title Scene UI, Build Profiles에 두 씬 등록
- [ ] 이상현상별 침식량·각성 단계(`minActivationLevel`) 밸런싱
- [ ] 추가 이상현상 후보 4종 채택 회의
- [ ] 텍스처·에셋 적용 후 고해상도 스크린샷 (포스터용)
- [ ] 그래픽팀 포스터 제작 (원고 전달 완료, 인게임 스크린샷 4종 전달 가능)
- [ ] 세이브/체크포인트에 정신력·해리성 단계 저장 연동
- [ ] BGM/SFX 볼륨 분리 (AudioMixer 도입 시 SettingsManager.ApplyVolume 수정)

---

## 포스터 관련
- 양식: A1 / PNG / 300DPI / 폼보드 600×900mm
- 하단 필수: 오투(O2) 로고 + "I2P X O2CUBE" 문구
- 원고 파일: `바탕화면\LucidDream_포스터_내용_원고.pdf`
- 그래픽팀이 디자인 담당 (내용 원고만 전달)
