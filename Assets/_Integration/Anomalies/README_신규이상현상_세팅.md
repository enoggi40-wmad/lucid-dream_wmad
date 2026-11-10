# 신규 이상현상 3종 — 세팅 가이드

추가 후보 중 **거꾸로 걸린 시계 / 책상 위의 손 / 거울 속 지연** 3종을 제작했다.
("끝나지 않는 복도"는 요청대로 제외.) 모두 연출 전용 MonoBehaviour이며, 기존
시스템과의 연동(Sanity 침식·단계별 등장/잠복·정화·진행도 집계)은 통합 컴포넌트가 담당한다.

## 연동 구조 (공통)

각 이상현상 GameObject = **연출 스크립트** + `AnomalyThreat` + `AnomalyPurifiable` + `Collider`.

| 컴포넌트 | 역할 |
|---|---|
| 연출 스크립트 | 시각/동작 연출만 담당. `AnomalyThreat`가 등장/잠복에 맞춰 자동 enable/disable |
| `AnomalyThreat` | 등장 중 + 플레이어가 위협 반경 안일 때 Sanity 침식. 현재 해리성 단계의 활성 등급과 `difficulty`가 일치할 때만 등장 |
| `AnomalyPurifiable` | 정화 빔 완료 시 → 이상현상 무력화 + Sanity 회복(효율 보정) + (옵션) 단계 완화 |
| `AnomalyDirector` | 씬의 모든 `AnomalyPurifiable` 자동 집계 → 전부 정화 시 게임 클리어 |

연출 스크립트는 **잠복/정화로 비활성화되면 정상 상태로 복귀**하도록 만들어져 있다(정화 후 시계가 바로 서고, 손이 제자리로, 반사 더미가 사라짐).

## 한 번에 배선하기 (에디터 메뉴)

Hierarchy에서 대상 오브젝트(시계/손/거울)를 선택하고:

- **Tools ▸ Lucid Dream ▸ Anomalies ▸ Make Reversed Clock (selected)** — 난이도 중(Medium)
- **Tools ▸ Lucid Dream ▸ Anomalies ▸ Make Desk Hand (selected)** — 난이도 상(Hard)
- **Tools ▸ Lucid Dream ▸ Anomalies ▸ Make Mirror Delay (selected)**  — 난이도 상(Hard)

→ 연출 + AnomalyThreat(+난이도) + AnomalyPurifiable + BoxCollider가 한 번에 붙는다.
난이도(등장 단계)는 AnomalyThreat 인스펙터에서 언제든 변경 가능.

> 참고: 단계↑(해리 심화)일수록 Easy(하) 등장, 단계↓(자각)일수록 Hard(상) 등장.

## 이상현상별 연출 필드

### 1) 거꾸로 걸린 시계 — `ReversedClockAnomaly`
- `hourHand/minuteHand/secondHand`: 바늘 Transform (역방향 회전)
- `clockBody` + `hangUpsideDown`: 등장 시 몸체 180도 뒤집기
- 연동: 해리성 단계가 깊을수록·정신력이 낮을수록 바늘이 더 빨라짐(옵션)

### 2) 책상 위의 손 — `DeskHandAnomaly`
- `hand`: 이동시킬 손 Transform / `fingers[]`: 까딱일 손가락 관절
- `crawlDirectionLocal`, `crawlSpeed`, `maxCrawlDistance`: 기어오기
- `freezeWhenObserved`: 플레이어가 쳐다보면 멈춤(위핑 엔젤식). `observeConeDeg`로 시야각 판정
- 연동: 단계가 깊을수록 기어오는 속도↑

### 3) 거울 속 지연 — `MirrorDelayAnomaly`
- 거울 표면 오브젝트에 부착. `mirrorPlane`(비우면 자신) + `planeNormalLocal`로 거울 평면 정의
- `reflectionProxy`: 플레이어를 닮은 더미(반사상)를 연결 → 거울 너머에 지연 배치
- `delaySeconds` + `increaseDelayWhenLowSanity`: 정신력이 낮을수록 반사가 더 늦게 따라옴
- `target` 비우면 SanityManager/Player 태그로 플레이어 자동 탐색

## 배치 체크리스트
1. 대상 오브젝트 배치(시계/손 모델/거울 + 반사 더미).
2. 위 메뉴로 배선.
3. 연출 필드(바늘·손가락·반사 더미 등) 인스펙터에서 연결.
4. `AnomalyThreat`의 `threatRange`·`baseSanityDrainPerSecond`, 난이도 확인.
5. 씬에 `AnomalyDirector`가 있으면 자동 집계됨(없으면 Build In-Game UI로 생성).
