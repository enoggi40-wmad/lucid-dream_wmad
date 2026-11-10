# LUCID DREAM 프로토타입 세팅 가이드 (7/2 목표)

스크립트는 전부 작성되어 있고, Unity 에디터에서 아래 순서대로 **메뉴 클릭**만 하면
"타이틀 → 게임 → 정화 → 클리어/게임오버"가 도는 프로토타입이 완성된다.

## 1. 게임 씬 세팅 (School 씬)

1. `Assets/yujin/Scenes/School.unity` 를 연다.
2. 플레이어 프리팹(PlayerController + PurifyController 포함)을 씬에 배치하고 태그를 `Player` 로 설정한다.
3. 메뉴 **Tools ▸ Lucid Dream ▸ Wire Anomalies In Open Scene**
   → 씬의 WJ_Anomaly_* 오브젝트에 AnomalyThreat(정신력 침식) 자동 부착.
4. 정화 대상으로 만들 이상현상 오브젝트들을 Hierarchy에서 선택한 뒤
   메뉴 **Tools ▸ Lucid Dream ▸ Make Selected Purifiable Anomaly**
   → AnomalyPurifiable(정화 완료 시 정신력 회복 + 무력화) + Collider 자동 부착.
5. 메뉴 **Tools ▸ Lucid Dream ▸ UI ▸ Build In-Game UI**
   → 아래가 전부 자동 생성·배선된다:
   - 정신력 바 + 해리성 단계 표시 + 저정신력 비네트 (SanityUI)
   - 이상현상 정화 카운터 + 중앙 알림 (GameHUD)
   - 일시정지 메뉴(ESC) + 설정 패널 (PauseMenuUI / SettingsPanelUI)
   - 클리어·게임오버 화면 (GameResultUI)
   - GameFlowManager / AnomalyDirector / SanityManager / DissociationLevelManager / GameManager
     (씬에 없으면 자동 생성. SanityManager는 Player 태그 오브젝트에 붙는다)

## 2. 타이틀 씬 세팅

1. 새 씬을 만들고 이름을 **Title** 로 저장한다. (`GameFlowManager`의 titleSceneName 기본값)
2. 메뉴 **Tools ▸ Lucid Dream ▸ UI ▸ Build Title Scene UI**
   → 타이틀 텍스트 + 시작/설정/종료 버튼 + 설정 패널 자동 생성.
   시작 버튼은 기본으로 `School` 씬을 로드한다 (TitleScreenUI 인스펙터에서 변경 가능).

## 3. 빌드 설정

**File ▸ Build Profiles** 의 Scene List에 `Title`, `School` 두 씬을 등록한다 (Title이 첫 번째).

## 게임 루프 (연동 구조)

```
이상현상(AnomalyThreat) ──침식──▶ 정신력(SanityManager)
        ▲                           │ 30% 이하
        │ 각성(minActivationLevel)   ▼
해리성 단계(DissociationLevelManager) ◀─강제 상승─┘   (악순환)

정화 빔(PurifyController) ──정화 완료──▶ AnomalyPurifiable
   → 이상현상 무력화 + 정신력 회복(정화 효율 보정) + (옵션) 단계 완화   (선순환)

AnomalyDirector: 씬의 모든 AnomalyPurifiable 추적 → 전부 정화되면
GameFlowManager: 클리어 처리. Sanity 0 → GameManager가 체크포인트 부활
  (GameFlowManager.maxRespawns 를 0 이상으로 설정하면 횟수 소진 시 게임 오버)
```

## 조작

| 입력 | 동작 |
|---|---|
| WASD / 마우스 | 이동 / 시점 |
| 우클릭 (유지) | 정화 빔 |
| ESC | 일시정지 / 재개 |

## 설정 항목 (PlayerPrefs 자동 저장)

전체 볼륨 · 마우스 감도(PlayerController 배수) · 전체 화면 · 해상도 · 그래픽 품질

## 알려진 한계 (프로토타입 범위)

- 볼륨은 마스터(AudioListener) 단일 채널. BGM/SFX 분리는 AudioMixer 도입 후 `SettingsManager.ApplyVolume()`만 수정하면 된다.
- 정화 빔을 쏘는 도중 ESC로 일시정지하면 빔 이펙트가 화면에 남을 수 있다 (재개 시 정상).
- 자동 생성된 UI는 전부 일반 UGUI 오브젝트라서 그래픽 팀이 자유롭게 디자인을 교체할 수 있다.
